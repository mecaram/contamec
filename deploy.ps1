param(
    [string]$Configuration = "Release",
    [string]$ArtifactsRoot = ".\artifacts\plesk",
    [string]$ZipPath = ".\artifacts\ContaMec-Godaddy-Plesk.zip",
    [switch]$SkipNpmInstall,
    # GoDaddy compartido no suele tener runtime ASP.NET Core: publicamos autocontenido (pesado pero portable).
    [switch]$FrameworkDependentPublish
)

$ErrorActionPreference = "Stop"

function Invoke-Step {
    param(
        [string]$Message,
        [scriptblock]$Action
    )

    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
    & $Action
}

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$backendProject = Join-Path $root "backend\ContaMec.Api\ContaMec.Api.csproj"
$frontendProjectDir = Join-Path $root "frontend\ContaMec.Web"
$frontendDistDir = Join-Path $frontendProjectDir "dist\contamec-web"
$backendPublishDir = Join-Path $ArtifactsRoot "backend-publish-temp"
$stagingDir = Join-Path $ArtifactsRoot "plesk-deploy-root"

if (-not (Test-Path $ArtifactsRoot)) {
    New-Item -ItemType Directory -Path $ArtifactsRoot -Force | Out-Null
}

Invoke-Step "Limpiando carpetas temporales y ZIP previo" {
    if (Test-Path $stagingDir) { Remove-Item -Recurse -Force $stagingDir }
    if (Test-Path $backendPublishDir) { Remove-Item -Recurse -Force $backendPublishDir }
    if (Test-Path $ZipPath) { Remove-Item -Force $ZipPath }
    New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null
    New-Item -ItemType Directory -Path $backendPublishDir -Force | Out-Null
}

$publishArgs = @(
    "publish",
    $backendProject,
    "-c", $Configuration,
    "-o", $backendPublishDir,
    "--verbosity", "minimal"
)

if (-not $FrameworkDependentPublish) {
    $publishArgs += @("-r", "win-x64", "--self-contained", "true")
    Write-Host "Backend: publicacion AUTOCONTENIDA win-x64 (recomendada para hosting sin ASP.NET Core Runtime)." -ForegroundColor Yellow
} else {
    Write-Host "Backend: publicacion dependiente del runtime (dotnet 8 debe estar instalado en el servidor)." -ForegroundColor Yellow
}

Invoke-Step "Publicando backend ASP.NET Core" {
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish fallo." }
    # Evita copiar al ZIP restos opcionales de una carpeta local backend\...\publish\
    $nestedPublish = Join-Path $backendPublishDir "publish"
    if (Test-Path $nestedPublish) {
        Remove-Item -Recurse -Force $nestedPublish
    }
}

if (-not $SkipNpmInstall) {
    Invoke-Step "Instalando dependencias frontend (npm install)" {
        Push-Location $frontendProjectDir
        $savedEap = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        npm install 2>&1 | ForEach-Object { Write-Host $_ }
        $code = $LASTEXITCODE
        $ErrorActionPreference = $savedEap
        Pop-Location
        if ($code -ne 0) { throw "npm install fallo (codigo $code)." }
    }
}

Invoke-Step "Compilando frontend Angular (production)" {
    Push-Location $frontendProjectDir
    $savedEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    npm run build -- --configuration production 2>&1 | ForEach-Object { Write-Host $_ }
    $code = $LASTEXITCODE
    $ErrorActionPreference = $savedEap
    Pop-Location
    if ($code -ne 0) { throw "npm run build fallo (codigo $code)." }
}

Invoke-Step "Armando contenido para subir a la raiz del subdominio" {
    Copy-Item -Path (Join-Path $frontendDistDir "*") -Destination $stagingDir -Recurse -Force
    New-Item -ItemType Directory -Path (Join-Path $stagingDir "api") -Force | Out-Null
    Copy-Item -Path (Join-Path $backendPublishDir "*") -Destination (Join-Path $stagingDir "api") -Recurse -Force
}

Invoke-Step "Creando LEEME de despliegue dentro del paquete" {
    $notaTipo = if ($FrameworkDependentPublish) {
        "Este PAQUETE usa publicacion FRAMEWORK-DEPENDIENTE; en el servidor debe estar instalado el ASP.NET Core 8 Hosting Bundle para IIS."
    } else {
        "Este PAQUETE incluye la Carpeta api con runtime EMBEBIDO (publicacion AUTOCONTENIDA win-x64). No depende del runtime global de .NET 8 instalado en el servidor, pero IIS sigue necesitando el modulo AspNetCore (Hosting Bundle en muchos planes) para procesar web.config correctamente."
    }

    @"
================================================================================
ContaMec - Paquete para GoDaddy / Windows / Plesk
================================================================================

Aclaracion: plan ''.NET Framework 4.8 integrado''
------------------------------------------------------------
El navegador y el front siguen sirviendo archivos estaticos como cualquier web.
La API NO es ''.NET Framework 4.8'': es ASP.NET Core (net8).
El ''.NET integrado'' 4.x en el panel es OTRO modelo (System.Web/IIS clasico).

$notaTipo

Contenido de este ZIP
---------------------
Raiz extraida del ZIP = DOCUMENT ROOT del subdominio (donde debe quedar index.html).
Carpeta \api = backend publicado para montarse como subaplicacion en la ruta /api.

El front ya usa apiBaseUrl = '/api' (mismo host que el Angular).

Pasos en Plesk (resumen)
------------------------
1. Haga backup de lo que tiene en la carpeta del subdominio.
2. Suba ContaMec-Godaddy-Plesk.zip con el '+' del administrador de archivos.
3. Menu ''Archive'' -> ''Extract'' y extraiga todo en esta misma RAIZ del subdominio.
4. IMPORTANTE: en IIS/Plesk la carpeta ''api'' debe ser una aplicacion/virtual application
   con alias ''api'', apuntando a la carpeta fisica ''...\api\'', con grupo de aplicaciones
   adecuado ( ''.NET CLR'' puede ser ''Sin codigo administrado'' / No Managed Code para Core).
5. Configure cadena SQL, JWT etc. editando los archivos en la carpeta \api\appsettings*.json (o variables de entorno si Plesk las permite).
6. Requiere URL Rewrite instalado para el web.config Angular en la raiz.

Si la API devuelve 500 o IIS no ejecuta la carpeta api, suele ser: falta subaplicacion,
falta Hosting Bundle/modulo AspNetCore, o restricciones del plan compartido.

Generado ejecutando desde el PC: .\deploy.ps1
================================================================================
"@ | Set-Content -Path (Join-Path $stagingDir "LEEME-DEPLOY-PLESK.txt") -Encoding UTF8
}

Invoke-Step "Creando archivo ZIP para subir" {
    Compress-Archive -Path (Join-Path $stagingDir "*") -DestinationPath $ZipPath -Force
}

Write-Host ""
Write-Host "Listo." -ForegroundColor Green
Write-Host "  ZIP para subir: $((Resolve-Path $ZipPath).Path)" -ForegroundColor Green
Write-Host "  (Staging sin comprimir por si necesita revisar: $stagingDir)" -ForegroundColor DarkGray
