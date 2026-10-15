# Deploy ContaMec.Api en Render (Docker)

La API .NET 8 corre en [Render](https://render.com) con Docker y se conecta a la base de Supabase.
El frontend corre en Vercel (ver `DEPLOY_VERCEL.md`) y llama a esta API.

| Pieza | Dónde |
|-------|--------|
| Base de datos | Supabase (`postgres`, Session pooler) |
| API ASP.NET Core | Render (`render.yaml` + `backend/ContaMec.Api/Dockerfile`) |
| Angular (UI) | Vercel |

## 1) Crear el servicio (una sola vez)

1. Entrá a [dashboard.render.com](https://dashboard.render.com) con tu cuenta de GitHub.
2. **New → Blueprint** → elegí el repo `mecaram/contamec` (rama `main`).
3. Render lee `render.yaml` y propone el servicio **contamec-api** (plan Free).
4. Te pide el valor de `ConnectionStrings__DefaultConnection`. Pegá la cadena del Session pooler de Supabase:

   ```
   Host=aws-1-ca-central-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.blgpwmcolswofufwkwml;Password=<password>;SSL Mode=Require
   ```

   `Jwt__Key` la genera Render sola (valor aleatorio). No hace falta tocarla.
5. **Apply**. El primer build tarda unos minutos.

## 2) Verificar

- URL del servicio: `https://contamec-api.onrender.com` (si Render asigna otro nombre, usá ese y actualizá `frontend/ContaMec.Web/src/environments/environment.vercel.ts`).
- `https://contamec-api.onrender.com/` muestra Swagger.
- `POST https://contamec-api.onrender.com/api/auth/login` con `{"username":"admin","password":"..."}` devuelve un token.

## 3) Deploys siguientes

Cada push a `main` que cambie algo en `backend/` redeploya la API automáticamente.

## Notas

- **Plan Free:** el servicio se duerme tras ~15 min sin uso; la primera petición después tarda ~1 minuto en despertar.
- Las variables se cambian en Render → contamec-api → **Environment** (luego **Save, rebuild and deploy**).
- Si cambia `Jwt__Key`, los usuarios logueados tienen que volver a iniciar sesión.
