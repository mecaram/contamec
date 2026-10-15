// Build Vercel: el front vive en *.vercel.app; la API ASP.NET Core corre en Render (ver render.yaml).
// Si Render asigna otra URL al servicio contamec-api, cambiala acá (debe ser HTTPS válido).
export const environment = {
  production: true,
  apiBaseUrl: 'https://contamec-api.onrender.com/api'
};
