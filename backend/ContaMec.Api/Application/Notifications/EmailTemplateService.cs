using ContaMec.Api.Domain.Entities;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Net;
using System.Text.RegularExpressions;

namespace ContaMec.Api.Application.Notifications;

public class EmailTemplateService(ContaMecDbContext dbContext, ILogger<EmailTemplateService> logger) : IEmailTemplateService
{
    private static readonly Regex Placeholder = new(@"\{\{\s*(\w+)\s*\}\}", RegexOptions.Compiled);

    public async Task<RenderedEmail?> RenderAsync(TemplateType type, IReadOnlyDictionary<string, string?> values, CancellationToken cancellationToken = default)
    {
        EmailTemplate? template;
        try
        {
            template = await dbContext.EmailTemplates
                .AsNoTracking()
                .Where(t => t.Name == type)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException or OperationCanceledException)
        {
            // Se envía después de confirmar la operación: una plantilla ilegible no debe convertirla en error.
            logger.LogWarning(ex, "No se pudo leer la plantilla de correo {Template}; se usa el texto por defecto.", type);
            return null;
        }

        if (string.IsNullOrWhiteSpace(template?.Subject) || string.IsNullOrWhiteSpace(template.Body))
        {
            logger.LogWarning("No hay plantilla de correo {Template} completa en EmailTemplates; se usa el texto por defecto.", type);
            return null;
        }

        // El asunto es texto plano; el cuerpo es HTML.
        return new RenderedEmail(
            Replace(template.Subject, values, htmlEncode: false),
            Replace(template.Body, values, htmlEncode: true));
    }

    /// <summary>Un marcador sin valor conocido queda tal cual, para que se note en el correo y se corrija la plantilla.</summary>
    private static string Replace(string text, IReadOnlyDictionary<string, string?> values, bool htmlEncode)
    {
        return Placeholder.Replace(text, match =>
        {
            if (!values.TryGetValue(match.Groups[1].Value, out var value))
                return match.Value;
            value ??= string.Empty;
            return htmlEncode ? WebUtility.HtmlEncode(value) : value;
        });
    }
}
