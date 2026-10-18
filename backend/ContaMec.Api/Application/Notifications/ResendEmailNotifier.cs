using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace ContaMec.Api.Application.Notifications;

public class ResendEmailNotifier(
    HttpClient httpClient,
    IOptions<NotificationSettings> notificationOptions,
    IOptions<ResendSettings> resendOptions,
    ILogger<ResendEmailNotifier> logger) : IEmailNotifier
{
    private const string Endpoint = "https://api.resend.com/emails";
    private readonly NotificationSettings _notifications = notificationOptions.Value;
    private readonly ResendSettings _resend = resendOptions.Value;

    public async Task NotifyAsync(string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_notifications.Email) || string.IsNullOrWhiteSpace(_resend.ApiKey))
        {
            logger.LogWarning("Aviso por correo no enviado: falta configurar Notifications:Email o Resend:ApiKey.");
            return;
        }

        var from = string.IsNullOrWhiteSpace(_resend.FromName)
            ? _resend.FromAddress
            : $"{_resend.FromName} <{_resend.FromAddress}>";

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(new
            {
                from,
                to = new[] { _notifications.Email.Trim() },
                subject,
                html = htmlBody
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _resend.ApiKey.Trim());

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // Resend explica el rechazo en el cuerpo (p. ej. remitente no verificado o destinatario no permitido).
                var detail = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Resend rechazó el aviso por correo: {StatusCode} {Detail}", (int)response.StatusCode, detail);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "No se pudo enviar el aviso por correo a {Email}.", _notifications.Email);
        }
    }
}
