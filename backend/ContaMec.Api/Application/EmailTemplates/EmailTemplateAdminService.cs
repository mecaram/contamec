using ContaMec.Api.Application.EmailTemplates.Dto;
using ContaMec.Api.Domain.Entities;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ContaMec.Api.Application.EmailTemplates;

public class EmailTemplateAdminService(ContaMecDbContext dbContext) : IEmailTemplateAdminService
{
    private const int SubjectMaxLength = 200;
    private readonly ContaMecDbContext _dbContext = dbContext;

    public async Task<List<EmailTemplateResponse>> SearchAsync(EmailTemplateSearchRequest request)
    {
        request ??= new EmailTemplateSearchRequest();

        var query = _dbContext.EmailTemplates.AsNoTracking().AsQueryable();

        if (request.Id.HasValue)
            query = query.Where(t => t.Id == request.Id.Value);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = ParseName(request.Name);
            query = query.Where(t => t.Name == name);
        }

        return await query
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .Select(MapToResponse())
            .ToListAsync();
    }

    public async Task<EmailTemplateResponse?> GetByIdAsync(int id)
    {
        return await _dbContext.EmailTemplates
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(MapToResponse())
            .FirstOrDefaultAsync();
    }

    public async Task<EmailTemplateResponse> CreateAsync(EmailTemplateSaveRequest request)
    {
        var (name, subject, body) = await ValidateAsync(request, null);

        var template = new EmailTemplate
        {
            Name = name,
            Subject = subject,
            Body = body,
            CreatedAt = ArgentinaClock.Now
        };

        _dbContext.EmailTemplates.Add(template);
        await _dbContext.SaveChangesAsync();

        var created = await GetByIdAsync(template.Id);
        return created ?? throw new InvalidOperationException("No se pudo leer la plantilla recién creada.");
    }

    public async Task<bool> UpdateAsync(int id, EmailTemplateSaveRequest request)
    {
        var (name, subject, body) = await ValidateAsync(request, id);

        var template = await _dbContext.EmailTemplates.FirstOrDefaultAsync(t => t.Id == id);
        if (template is null)
            return false;

        template.Name = name;
        template.Subject = subject;
        template.Body = body;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var template = await _dbContext.EmailTemplates.FirstOrDefaultAsync(t => t.Id == id);
        if (template is null)
            return false;

        // Sin plantilla, los correos vuelven al texto por defecto del código.
        _dbContext.EmailTemplates.Remove(template);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private static Expression<Func<EmailTemplate, EmailTemplateResponse>> MapToResponse()
    {
        return template => new EmailTemplateResponse
        {
            Id = template.Id,
            // Etiquetas del enum TemplateTypes de la base.
            Name = template.Name == TemplateType.PedidoDemo ? nameof(TemplateType.PedidoDemo)
                : template.Name == TemplateType.PedidoDemoAprobado ? nameof(TemplateType.PedidoDemoAprobado)
                : template.Name == TemplateType.PedidoProduccion ? nameof(TemplateType.PedidoProduccion)
                : template.Name == TemplateType.PedidoProduccionAprobado ? nameof(TemplateType.PedidoProduccionAprobado)
                : template.Name == TemplateType.PedidoRenovacion ? nameof(TemplateType.PedidoRenovacion)
                : template.Name == TemplateType.PedidoRenovacionAprobado ? nameof(TemplateType.PedidoRenovacionAprobado)
                : null,
            Subject = template.Subject,
            Body = template.Body,
            CreatedAt = template.CreatedAt
        };
    }

    private async Task<(TemplateType Name, string Subject, string Body)> ValidateAsync(EmailTemplateSaveRequest request, int? currentId)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Debe seleccionar el nombre de la plantilla.");
        var name = ParseName(request.Name);

        var subject = request.Subject?.Trim();
        if (string.IsNullOrEmpty(subject))
            throw new ArgumentException("El asunto es obligatorio.");
        if (subject.Length > SubjectMaxLength)
            throw new ArgumentException($"El asunto no puede superar {SubjectMaxLength} caracteres.");

        var body = request.Body?.Trim();
        if (string.IsNullOrEmpty(body))
            throw new ArgumentException("El cuerpo es obligatorio.");

        // El envío toma una sola plantilla por nombre: dos con el mismo nombre dejarían una sin efecto.
        var exists = await _dbContext.EmailTemplates
            .AnyAsync(t => t.Name == name && (!currentId.HasValue || t.Id != currentId.Value));
        if (exists)
            throw new ArgumentException("Ya existe una plantilla con ese nombre.");

        return (name, subject, body);
    }

    private static TemplateType ParseName(string name)
    {
        if (!Enum.TryParse<TemplateType>(name.Trim(), ignoreCase: true, out var value) || !Enum.IsDefined(value))
            throw new ArgumentException("El nombre de la plantilla no es válido.");
        return value;
    }
}
