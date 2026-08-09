# Deploy ContaMec (frontend) en Vercel

## Importante

Vercel aloja el **frontend Angular**.
La **API ASP.NET Core no se ejecuta en Vercel**.

Arquitectura recomendada:

| Pieza | Dónde |
|-------|--------|
| Angular (UI) | Vercel |
| ContaMec.Api (.NET 8) | GoDaddy/Plesk, Azure, Railway, etc. |

El front en Vercel llama a la API por HTTPS (ver `environment.vercel.ts`).

## Requisitos previos

1. Cuenta en [vercel.com](https://vercel.com) (GitHub conectado recomendado).
2. API publicada con **certificado SSL válido** (sin `ERR_CERT_AUTHORITY_INVALID`).
3. CORS en la API: ya permite orígenes (`AllowAnyOrigin` en ContaMec.Api).

## Opción A — Desde el dashboard de Vercel (recomendada)

1. New Project → importar `mecaram/contamec`.
2. **Root Directory:** `frontend/ContaMec.Web`
3. Framework Preset: Other
4. Build Command: `npm run build:vercel`
5. Output Directory: `dist/contamec-web`
6. Node.js Version: **16.x**
7. Deploy.

## Opción B — CLI

```powershell
nvm use 16.20.2
cd C:\Desarrollo\ContaMec\frontend\ContaMec.Web
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
apiBaseUrl: 'https://TU-API-PUBLICA/api'
```

Ejemplo si la API sigue en GoDaddy:

```ts
apiBaseUrl: 'https://contamec.miguelcaram.com/api'
```

Si el certificado de ese dominio no es de confianza, el login desde Vercel **fallará** aunque el front cargue bien. Primero arreglá SSL en Plesk (Let's Encrypt) o mové la API a un host con HTTPS válido.

## Plesk vs Vercel

- **Plesk / mismo dominio:** `environment.prod.ts` usa `apiBaseUrl: '/api'` (mismo origen).
- **Vercel:** `environment.vercel.ts` usa la URL absoluta de la API.
