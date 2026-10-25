/** Mismo límite y tipos que valida el backend. */
export const VOUCHER_MAX_BYTES = 10 * 1024 * 1024;
export const VOUCHER_ACCEPT = '.pdf,.jpg,.jpeg,.png,.webp,application/pdf,image/jpeg,image/png,image/webp';

const MIME_BY_EXTENSION: Record<string, string> = {
  pdf: 'application/pdf',
  jpg: 'image/jpeg',
  jpeg: 'image/jpeg',
  png: 'image/png',
  webp: 'image/webp'
};

/** Las imágenes se achican a este ancho como máximo, manteniendo la proporción. */
const MAX_IMAGE_WIDTH = 1600;
const JPEG_QUALITY = 0.8;

/** Mensaje de error si el archivo no se puede adjuntar; null si es válido. El tamaño se valida sobre el original. */
export function validateVoucherFile(file: File): string | null {
  const extension = file.name.split('.').pop()?.toLowerCase() ?? '';
  const mime = MIME_BY_EXTENSION[extension];
  if (!mime || file.type.toLowerCase() !== mime) {
    return 'El comprobante debe ser un PDF o una imagen (JPG, PNG o WEBP).';
  }
  if (file.size > VOUCHER_MAX_BYTES) {
    return `El archivo pesa ${formatFileSize(file.size)} y el máximo es 10 MB.`;
  }
  return null;
}

export function isImageFile(file: File): boolean {
  return file.type.startsWith('image/');
}

/**
 * Las imágenes se redimensionan y pasan a JPEG en el navegador, con Canvas, antes de subirlas.
 * Los PDF se suben sin modificar. Si la versión comprimida no es más liviana, se sube la original.
 */
export async function prepareVoucherFile(file: File): Promise<File> {
  if (!isImageFile(file)) return file;

  const image = await loadImage(file);
  const scale = Math.min(1, MAX_IMAGE_WIDTH / image.naturalWidth);
  const width = Math.round(image.naturalWidth * scale);
  const height = Math.round(image.naturalHeight * scale);

  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  const context = canvas.getContext('2d');
  if (!context) return file;
  // JPEG no tiene transparencia: el fondo de los PNG queda blanco en lugar de negro.
  context.fillStyle = '#fff';
  context.fillRect(0, 0, width, height);
  context.drawImage(image, 0, 0, width, height);

  const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', JPEG_QUALITY));
  if (!blob || blob.size >= file.size) return file;

  const baseName = file.name.replace(/\.[^.]+$/, '');
  return new File([blob], `${baseName}.jpg`, { type: 'image/jpeg' });
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1).replace('.', ',')} MB`;
}

function loadImage(file: File): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const url = URL.createObjectURL(file);
    const image = new Image();
    image.onload = () => {
      URL.revokeObjectURL(url);
      resolve(image);
    };
    image.onerror = () => {
      URL.revokeObjectURL(url);
      reject(new Error('No se pudo leer la imagen.'));
    };
    image.src = url;
  });
}
