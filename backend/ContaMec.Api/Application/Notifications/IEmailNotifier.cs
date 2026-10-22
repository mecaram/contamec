namespace ContaMec.Api.Application.Notifications;

/// <summary>Avisos por correo, por defecto al destinatario configurado en <see cref="NotificationSettings.Email"/>.</summary>
public interface IEmailNotifier
{
    /// <summary>Envía el correo; nunca lanza excepción: un aviso fallido se registra en el log y no interrumpe la operación.</summary>
    Task NotifyAsync(string subject, string htmlBody, CancellationToken cancellationToken = default);

    /// <summary>Igual que <see cref="NotifyAsync"/>, pero a <paramref name="to"/>. Devuelve si Resend aceptó el correo.</summary>
    Task<bool> SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
