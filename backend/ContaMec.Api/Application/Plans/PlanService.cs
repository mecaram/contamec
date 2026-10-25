using ContaMec.Api.Application.Plans.Dto;
using ContaMec.Api.Domain.Entities;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContaMec.Api.Application.Plans;

public class PlanService(ContaMecDbContext dbContext) : IPlanService
{
    /// <summary>
    /// Todas las empresas, activas o no, con su plan. Vigente es hasta el día de vencimiento inclusive
    /// (fecha de Argentina), igual que el control del login.
    /// </summary>
    public async Task<List<PlanResponse>> SearchAsync(PlanSearchRequest request)
    {
        request ??= new PlanSearchRequest();
        var today = ArgentinaClock.Now.Date;

        var query = dbContext.Applications.AsNoTracking().AsQueryable();

        if (request.ApplicationId.HasValue)
            query = query.Where(a => a.Id == request.ApplicationId.Value);

        if (!string.IsNullOrWhiteSpace(request.ApplicationName))
        {
            var name = request.ApplicationName.Trim().ToLower();
            query = query.Where(a => a.Name.ToLower().Contains(name));
        }

        if (!string.IsNullOrWhiteSpace(request.Locality))
        {
            var locality = request.Locality.Trim();
            query = query.Where(a => a.Locality != null && a.Locality.Trim() == locality);
        }

        if (request.Active.HasValue)
            query = query.Where(a => a.Active == request.Active.Value);

        query = request.Plan switch
        {
            nameof(ApplicationType.Demo) => query.Where(a => a.Type == ApplicationType.Demo),
            nameof(ApplicationType.ProPlus) => query.Where(a => a.Type == ApplicationType.ProPlus),
            "none" => query.Where(a => a.Type == null),
            _ => query
        };

        query = request.Expiration switch
        {
            "current" => query.Where(a => a.ApplicationExpirationDate >= today),
            "expired" => query.Where(a => a.ApplicationExpirationDate < today),
            "none" => query.Where(a => a.ApplicationExpirationDate == null),
            _ => query
        };

        var rows = await query
            .OrderBy(a => a.Name)
            .ThenBy(a => a.Id)
            .ToListAsync();

        return rows.Select(a => ToResponse(a, today)).ToList();
    }

    private const int DemoDays = 7;

    /// <summary>
    /// Activa la empresa y sus usuarios y le asigna el plan Demo por <see cref="DemoDays"/> días más:
    /// sin vencimiento o vencido, desde hoy; vigente (vence hoy o después), desde su vencimiento actual.
    /// </summary>
    public async Task<PlanResponse?> ActivateDemoAsync(int applicationId)
    {
        var application = await dbContext.Applications
            .Include(a => a.Users)
            .FirstOrDefaultAsync(a => a.Id == applicationId);
        if (application is null)
            return null;

        var now = ArgentinaClock.Now;
        var expiresAt = application.ApplicationExpirationDate;
        var isCurrent = expiresAt.HasValue && expiresAt.Value.Date >= now.Date;

        // El inicio del plan solo cambia si empieza uno nuevo; al extender una Demo vigente se conserva.
        if (application.Type != ApplicationType.Demo || !isCurrent)
            application.ApplicationTypeDate = now;

        application.Type = ApplicationType.Demo;
        application.ApplicationExpirationDate = isCurrent ? expiresAt!.Value.AddDays(DemoDays) : now.AddDays(DemoDays);
        application.Active = true;
        application.Guid ??= System.Guid.NewGuid().ToString();
        foreach (var user in application.Users)
            user.IsActive = true;

        // Un único SaveChanges: empresa, usuarios y plan se guardan juntos o no se guarda nada.
        await dbContext.SaveChangesAsync();

        return ToResponse(application, now.Date);
    }

    private static PlanResponse ToResponse(Domain.Entities.Application a, DateTime today) => new()
    {
        ApplicationId = a.Id,
        ApplicationName = a.Name,
        Address = a.Address,
        Locality = a.Locality,
        WhatsApp = a.WhatsApp,
        Active = a.Active,
        Plan = ApplicationTypeNames.Display(a.Type),
        StartDate = a.ApplicationTypeDate,
        ExpirationDate = a.ApplicationExpirationDate,
        DaysLeft = a.ApplicationExpirationDate.HasValue ? (a.ApplicationExpirationDate.Value.Date - today).Days : null
    };

    /// <summary>Localidades distintas de las empresas (sin espacios sobrantes ni vacías), en orden alfabético.</summary>
    public async Task<List<string>> GetLocalitiesAsync()
    {
        var localities = await dbContext.Applications
            .AsNoTracking()
            .Where(a => a.Locality != null && a.Locality.Trim() != "")
            .Select(a => a.Locality!.Trim())
            .Distinct()
            .ToListAsync();

        // Orden en español: las vocales con tilde van junto a las sin tilde.
        var comparer = StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("es-AR"), ignoreCase: true);
        return localities.OrderBy(l => l, comparer).ToList();
    }
}
