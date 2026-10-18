namespace ContaMec.Api.Application.Notifications;

/// <summary>
/// Cuenta de Resend (resend.com) para el correo saliente. Sin dominio verificado, el remitente debe ser onboarding@resend.dev
/// y solo se puede enviar al correo con el que se creó la cuenta.
/// </summary>
public class ResendSettings
{
    public const string SectionName = "Resend";

    /// <summary>Secreto: va en User Secrets o en variables de entorno, nunca en appsettings.json.</summary>
    public string? ApiKey { get; set; }

    public string FromAddress { get; set; } = "onboarding@resend.dev";
    public string? FromName { get; set; }
}
