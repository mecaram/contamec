# Deploy en GoDaddy (Windows + Plesk)

## 1) Generar paquetes de publicacion en local

Desde la raiz del repo:

```powershell
.\deploy.ps1
```

Esto crea:

- `artifacts/plesk/frontend-site` (Angular listo para `httpdocs`)
- `artifacts/plesk/backend-api` (ASP.NET Core listo para app `/api`)

## 2) Subir frontend

En Plesk (dominio principal):

1. Ir a **Files** del dominio.
2. Abrir carpeta `httpdocs`.
3. Subir el contenido de `artifacts/plesk/frontend-site` (no la carpeta, su contenido).

## 3) Subir backend

En Plesk:

1. Crear una aplicacion IIS o Virtual Directory en ruta `/api`.
2. Asignar como carpeta fisica una nueva carpeta (por ejemplo `httpdocs_api`).
3. Subir contenido de `artifacts/plesk/backend-api` a esa carpeta.

## 4) Configurar appsettings de produccion

En el servidor, configurar valores reales (archivo o variables de entorno):

- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`

## 5) Revisar permisos y runtime

- Verificar que el hosting tenga runtime **.NET 8**.
- Si no esta disponible, usar publicacion self-contained.

## 6) Pruebas

1. Frontend: `https://tudominio.com`
2. API: `https://tudominio.com/api/...`
3. Login + operaciones CRUD.

## 7) Notas importantes

- El frontend usa `apiBaseUrl: '/api'` en desarrollo para proxy local.
- En produccion usa `environment.prod.ts` con `https://your-api-url/api`.
- Si vas a usar mismo dominio en produccion, reemplaza por `'/api'`.
