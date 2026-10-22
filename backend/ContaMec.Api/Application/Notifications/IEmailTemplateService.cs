using ContaMec.Api.Domain.Entities;

namespace ContaMec.Api.Application.Notifications;

public interface IEmailTemplateService
{
    /// <summary>
    /// Lee la plantilla de la tabla EmailTemplates y reemplaza sus marcadores {{Clave}} por <paramref name="values"/>
    /// (codificados como HTML en el cuerpo). Null si no hay plantilla completa o no se pudo leer: el llamador usa su texto por defecto.
    /// </summary>
    Task<RenderedEmail?> RenderAsync(TemplateType type, IReadOnlyDictionary<string, string?> values, CancellationToken cancellationToken = default);
}

public record RenderedEmail(string Subject, string Body);
