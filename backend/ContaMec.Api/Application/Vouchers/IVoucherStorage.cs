namespace ContaMec.Api.Application.Vouchers;

public interface IVoucherStorage
{
    Task UploadAsync(string path, byte[] content, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(IReadOnlyCollection<string> paths, CancellationToken cancellationToken = default);
    Task<string> CreateSignedUrlAsync(string path, CancellationToken cancellationToken = default);
}
