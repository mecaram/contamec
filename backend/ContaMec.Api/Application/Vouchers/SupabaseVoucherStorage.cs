using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace ContaMec.Api.Application.Vouchers;

/// <summary>Cliente de la API REST de Supabase Storage (/storage/v1) para el bucket de comprobantes.</summary>
public class SupabaseVoucherStorage(
    HttpClient httpClient,
    IOptions<SupabaseStorageSettings> options,
    ILogger<SupabaseVoucherStorage> logger) : IVoucherStorage
{
    private readonly SupabaseStorageSettings _settings = options.Value;

    public async Task UploadAsync(string path, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"object/{_settings.VoucherBucket}/{path}");
        request.Content = new ByteArrayContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        // Cada archivo tiene una ruta nueva: nunca se pisa uno existente.
        request.Headers.Add("x-upsert", "false");

        using var response = await SendAsync(request, "No se pudo subir el comprobante.", cancellationToken);
    }

    public async Task DeleteAsync(IReadOnlyCollection<string> paths, CancellationToken cancellationToken = default)
    {
        if (paths.Count == 0)
            return;

        // Borrar una ruta que ya no existe no es un error: Storage la omite.
        using var request = CreateRequest(HttpMethod.Delete, $"object/{_settings.VoucherBucket}");
        request.Content = JsonContent.Create(new { prefixes = paths });

        using var response = await SendAsync(request, "No se pudo borrar el comprobante.", cancellationToken);
    }

    public async Task<string> CreateSignedUrlAsync(string path, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"object/sign/{_settings.VoucherBucket}/{path}");
        request.Content = JsonContent.Create(new { expiresIn = _settings.SignedUrlSeconds });

        using var response = await SendAsync(request, "No se pudo obtener el comprobante.", cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<SignedUrlResult>(cancellationToken);
        if (string.IsNullOrWhiteSpace(result?.SignedUrl))
            throw new VoucherStorageException("No se pudo obtener el comprobante.");

        // Storage devuelve la ruta relativa a /storage/v1.
        return $"{BaseUrl}/storage/v1{result.SignedUrl}";
    }

    private string BaseUrl => (_settings.Url ?? string.Empty).Trim().TrimEnd('/');

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(_settings.Url) || string.IsNullOrWhiteSpace(_settings.ServiceRoleKey))
        {
            logger.LogWarning("Comprobantes: falta configurar SupabaseStorage:Url o SupabaseStorage:ServiceRoleKey.");
            throw new VoucherStorageException("El almacenamiento de comprobantes no está configurado.");
        }

        var key = _settings.ServiceRoleKey.Trim();
        var request = new HttpRequestMessage(method, $"{BaseUrl}/storage/v1/{relativePath}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Headers.Add("apikey", key);
        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string errorMessage, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Supabase Storage no respondió: {Method} {Uri}", request.Method, request.RequestUri);
            throw new VoucherStorageException(errorMessage);
        }

        if (response.IsSuccessStatusCode)
            return response;

        // Storage explica el rechazo en el cuerpo (p. ej. bucket inexistente o tipo de archivo no permitido).
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogWarning("Supabase Storage rechazó {Method} {Uri}: {StatusCode} {Detail}",
            request.Method, request.RequestUri, (int)response.StatusCode, detail);
        response.Dispose();
        throw new VoucherStorageException(errorMessage);
    }

    private sealed class SignedUrlResult
    {
        [JsonPropertyName("signedURL")]
        public string? SignedUrl { get; set; }
    }
}
