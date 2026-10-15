# Deploy ContaMec (frontend) en Vercel

## Importante

Vercel aloja el **frontend Angular**.
La **API ASP.NET Core no se ejecuta en Vercel**.

Arquitectura recomendada:

| Pieza | Dónde |
|-------|--------|
| Angular (UI) | Vercel |
| ContaMec.Api (.NET 8) | Render (ver `DEPLOY_RENDER.md`) |
| Base de datos | Supabase |

El front en Vercel llama a la API por HTTPS (ver `environment.vercel.ts`).

## Requisitos previos

1. Cuenta en [vercel.com](https://vercel.com) (GitHub conectado recomendado).
2. API publicada en Render (`DEPLOY_RENDER.md`); Render ya da HTTPS válido.
3. CORS en la API: ya permite orígenes (`AllowAnyOrigin` en ContaMec.Api).

## Opción A — Desde el dashboard de Vercel (recomendada)

1. New Project → importar `mecaram/contamec`.
2. **Root Directory:** `frontend/ContaMec.Web`
3. Framework Preset: Other
4. Build Command: `npm run build:vercel`
5. Output Directory: `dist/contamec-web`
6. Node.js Version: **22.x** (lo toma de `engines` en `package.json`; el script `build:vercel` agrega `--openssl-legacy-provider`, que Angular 12 necesita en Node 17+)
7. Deploy.

## Opción B — CLI

```powershell
nvm use 22
cd C:\Desarrollo\ContaMec\contamec\frontend\ContaMec.Web
npm i -g vercel
vercel login
vercel
```

Seguí el asistente (link al proyecto / scope). Para producción:

```powershell
vercel --prod
```

## URL de la API

Editá antes de desplegar (o después y redeploy):

`frontend/ContaMec.Web/src/environments/environment.vercel.ts`

```ts
apiBaseUrl: 'https://contamec-api.onrender.com/api'
```

Si Render le asignó otra URL al servicio `contamec-api`, cambiala acá, hacé push y Vercel redeploya solo.

Nota: `npm run build:vercel` usa sintaxis de Linux (como en Vercel). En Windows, para probar local: `$env:NODE_OPTIONS='--openssl-legacy-provider'; npx ng build --configuration vercel`.

## Plesk vs Vercel

- **Plesk / mismo dominio:** `environment.prod.ts` usa `apiBaseUrl: '/api'` (mismo origen).
- **Vercel:** `environment.vercel.ts` usa la URL absoluta de la API.
