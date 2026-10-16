# ContaMec

Aplicación fullstack para gestión contable diaria.

## Arquitectura

```text
Navegador ──► Frontend Angular (Vercel) ──HTTPS──► API .NET 8 (Render, Docker) ──► PostgreSQL (Supabase)
```

| Pieza | Tecnología | Hosting |
|-------|------------|---------|
| Base de datos | PostgreSQL | Supabase (Session pooler) |
| API | ASP.NET Core 8, Entity Framework Core + Npgsql, JWT, BCrypt, Swagger | Render (Docker, plan Free) |
| Frontend | Angular 12 + Angular Material | Vercel |

El código está en GitHub (`mecaram/contamec`). Cada push a `main` redeploya automáticamente:

- **Render** reconstruye la API solo si cambia algo en `backend/`.
- **Vercel** reconstruye el frontend.

## Estructura

```text
contamec/
  backend/
    ContaMec.Api/      API REST (Domain, Application, Infrastructure, Controllers)
  frontend/
    ContaMec.Web/      SPA Angular
  render.yaml          Blueprint de Render para la API
```

## Configuración

La API lee esta configuración de `appsettings.json`, de *User Secrets* (en local) o de variables de entorno (en Render):

| Clave | Variable de entorno | Descripción |
|-------|---------------------|-------------|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | Cadena de conexión al Session pooler de Supabase |
| `Jwt:Key` | `Jwt__Key` | Clave de firma de los tokens JWT (mínimo 32 caracteres) |

Ejemplo de cadena de conexión:

```text
Host=aws-1-ca-central-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<ref>;Password=<password>;SSL Mode=Require
```

Todos los endpoints exigen un token JWT, salvo el login (`POST /api/auth/login`).

## Cómo ejecutar en local

### Backend

```bash
cd backend/ContaMec.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<cadena de Supabase>"
dotnet restore
dotnet run
```

Swagger queda disponible en la raíz de la API.

### Frontend

```bash
cd frontend/ContaMec.Web
npm install
npm start
```

Con Node 17 o superior, Angular 12 necesita `NODE_OPTIONS=--openssl-legacy-provider`.

## Deploy

- API en Render: [DEPLOY_RENDER.md](DEPLOY_RENDER.md)
- Frontend en Vercel: [DEPLOY_VERCEL.md](DEPLOY_VERCEL.md)
- Alternativa anterior en GoDaddy/Plesk (IIS): [DEPLOY_PLESK_WINDOWS.md](DEPLOY_PLESK_WINDOWS.md)

## Notas

- Los scripts de `backend/ContaMec.Api/Scripts/` son de **SQL Server** (versión anterior del proyecto) y no sirven en Supabase. El schema vigente es el de la base de Supabase.
- En el plan Free de Render, la API se duerme tras ~15 minutos sin uso y la primera petición posterior tarda ~1 minuto.
