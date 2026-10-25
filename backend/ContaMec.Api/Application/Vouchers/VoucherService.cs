using ContaMec.Api.Application.Vouchers.Dto;
using ContaMec.Api.Domain.Entities;
using ContaMec.Api.Infrastructure.Data;

namespace ContaMec.Api.Application.Vouchers;

public class VoucherService(
    ContaMecDbContext dbContext,
    IVoucherStorage storage,
    ILogger<VoucherService> logger) : IVoucherService
{
    public const long MaxBytes = 10 * 1024 * 1024;

    // Solo PDF e imágenes: la extensión, el tipo MIME y el contenido del archivo tienen que coincidir.
    private static readonly Dictionary<string, string> MimeByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    public async Task<VoucherResponse> SaveAsync(IHasVoucher record, string folder, VoucherFile file, CancellationToken cancellationToken = default)
    {
        var (extension, contentType) = Validate(file);
        var path = $"{folder}/{record.Id}/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension}";

        // Si falla la subida no se toca el registro.
        await storage.UploadAsync(path, file.Content, contentType, cancellationToken);

        var previous = record.Voucher;
        record.Voucher = path;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Si falla la base, el archivo recién subido quedaría huérfano: se borra.
            record.Voucher = previous;
            await DeleteFilesAsync([path], cancellationToken);
            throw;
        }

        // El anterior se borra solo cuando el nuevo ya quedó guardado.
        await DeleteFilesAsync([previous], cancellationToken);
        return new VoucherResponse { FileName = FileNameOf(path), IsImage = IsImage(path) };
    }

    public async Task<bool> RemoveAsync(IHasVoucher record, CancellationToken cancellationToken = default)
    {
        var path = record.Voucher;
        if (path is null)
            return false;

        // Primero Storage: si falla, el registro conserva la ruta y se puede reintentar (borrar dos veces no es error).
        await storage.DeleteAsync([path], cancellationToken);
        record.Voucher = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<VoucherUrlResponse> GetUrlAsync(string path, CancellationToken cancellationToken = default)
    {
        return new VoucherUrlResponse
        {
            Url = await storage.CreateSignedUrlAsync(path, cancellationToken),
            FileName = FileNameOf(path),
            IsImage = IsImage(path)
        };
    }

    public async Task DeleteFilesAsync(IEnumerable<string?> paths, CancellationToken cancellationToken = default)
    {
        var list = paths.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!).Distinct().ToList();
        if (list.Count == 0)
            return;

        try
        {
            await storage.DeleteAsync(list, cancellationToken);
        }
        catch (VoucherStorageException)
        {
            logger.LogWarning("Comprobantes huérfanos en Storage, borrarlos a mano: {Paths}", string.Join(", ", list));
        }
    }

    private static (string Extension, string ContentType) Validate(VoucherFile file)
    {
        if (file.Content.Length == 0)
            throw new ArgumentException("El archivo está vacío.");
        if (file.Content.Length > MaxBytes)
            throw new ArgumentException("El comprobante no puede superar los 10 MB.");

        var extension = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
        if (!MimeByExtension.TryGetValue(extension, out var contentType)
            || !string.Equals(file.ContentType?.Trim(), contentType, StringComparison.OrdinalIgnoreCase)
            || !MatchesSignature(extension, file.Content))
            throw new ArgumentException("El comprobante debe ser un PDF o una imagen (JPG, PNG o WEBP).");

        return (extension, contentType);
    }

    // Los primeros bytes de cada formato: evita aceptar otro tipo de archivo renombrado.
    private static bool MatchesSignature(string extension, byte[] content)
    {
        ReadOnlySpan<byte> bytes = content;
        return extension switch
        {
            ".pdf" => bytes.StartsWith("%PDF"u8),
            ".jpg" or ".jpeg" => bytes.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }),
            ".png" => bytes.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".webp" => bytes.Length >= 12 && bytes.StartsWith("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
    }

    private static string FileNameOf(string path) => path[(path.LastIndexOf('/') + 1)..];

    private static bool IsImage(string path) => Path.GetExtension(path).ToLowerInvariant() is not ".pdf";
}
