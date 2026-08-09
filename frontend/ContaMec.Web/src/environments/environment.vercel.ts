// Build Vercel: el front vive en *.vercel.app; la API ASP.NET Core NO corre en Vercel.
// Cambiá esta URL si tu API está en otro host (debe ser HTTPS válido).
export const environment = {
  production: true,
  apiBaseUrl: 'https://contamec.miguelcaram.com/api'
};
