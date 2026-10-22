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
        if (string.IsNullOrWhiteSpace(_notifications.Email))
        {
            logger.LogWarning("Aviso por correo no enviado: falta configurar Notifications:Email.");
            return;
        }

        await SendAsync(_notifications.Email, subject, htmlBody, cancellationToken);
    }

    public async Task<bool> SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(_resend.ApiKey))
        {
            logger.LogWarning("Correo no enviado: falta el destinatario o configurar Resend:ApiKey.");
            return false;
        }

        var from = string.IsNullOrWhiteSpace(_resend.FromName)
            ? _resend.FromAddress
            : $"{_resend.FromName} <{_resend.FromAddress}>";

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(new
            {
                from,
                to = new[] { to.Trim() },
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
                logger.LogWarning("Resend rechazó el correo a {Email}: {StatusCode} {Detail}", to, (int)response.StatusCode, detail);
                return false;
            }
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "No se pudo enviar el correo a {Email}.", to);
            return false;
        }
    }
}
