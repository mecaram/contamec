namespace ContaMec.Api.Application.Notifications;

public class NotificationSettings
{
    public const string SectionName = "Notifications";

    /// <summary>WhatsApp de los desarrolladores, solo dígitos: 549 + código de área y número.</summary>
    public string? WhatsApp { get; set; }

    /// <summary>Correo de los desarrolladores que recibe los avisos del sistema. Vacío, los avisos no se envían.</summary>
    public string? Email { get; set; }
}
