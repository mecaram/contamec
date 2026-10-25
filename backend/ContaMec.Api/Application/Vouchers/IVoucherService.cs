using ContaMec.Api.Application.Vouchers.Dto;
using ContaMec.Api.Domain.Entities;

namespace ContaMec.Api.Application.Vouchers;

/// <summary>
/// Subida, reemplazo y borrado de comprobantes en Storage, para cualquier tabla con <see cref="IHasVoucher"/>.
/// La carpeta identifica la tabla dentro del bucket: {carpeta}/{id}/{timestamp}.{ext}.
/// </summary>
public interface IVoucherService
{
    /// <summary>
    /// Sube el archivo y guarda su ruta en el registro (que debe estar trackeado). Si ya tenía comprobante, lo reemplaza
    /// y borra el anterior solo después de guardar.
    /// </summary>
    /// <exception cref="ArgumentException">El archivo no es PDF o imagen, o supera los 10 MB.</exception>
    /// <exception cref="VoucherStorageException">Storage rechazó la subida; el registro no cambia.</exception>
    Task<VoucherResponse> SaveAsync(IHasVoucher record, string folder, VoucherFile file, CancellationToken cancellationToken = default);

    /// <summary>Borra el archivo de Storage y deja Voucher en NULL. Devuelve false si el registro no tenía comprobante.</summary>
    Task<bool> RemoveAsync(IHasVoucher record, CancellationToken cancellationToken = default);

    Task<VoucherUrlResponse> GetUrlAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Borra los archivos de registros ya eliminados. Si Storage falla, solo lo registra en el log.</summary>
    Task DeleteFilesAsync(IEnumerable<string?> paths, CancellationToken cancellationToken = default);
}
