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
            nameof(ApplicationType.Demo) => query.Where(a => a.Plan == ApplicationType.Demo),
            nameof(ApplicationType.ProPlus) => query.Where(a => a.Plan == ApplicationType.ProPlus),
            "none" => query.Where(a => a.Plan == null),
            _ => query
        };

        query = request.Expiration switch
        {
            "current" => query.Where(a => a.PlanExpirationDate >= today),
            "expired" => query.Where(a => a.PlanExpirationDate < today),
            "none" => query.Where(a => a.PlanExpirationDate == null),
            _ => query
        };

        var rows = await query
            .OrderBy(a => a.Name)
            .ThenBy(a => a.Id)
            .ToListAsync();

        return rows.Select(a => ToResponse(a, today)).ToList();
    }

    private const int DemoDays = 7;
    private const int ProPlusDays = 30;

    public Task<PlanResponse?> ActivateDemoAsync(int applicationId) =>
        ActivatePlanAsync(applicationId, ApplicationType.Demo, DemoDays);

    public Task<PlanResponse?> ActivateProPlusAsync(int applicationId) =>
        ActivatePlanAsync(applicationId, ApplicationType.ProPlus, ProPlusDays);

    /// <summary>
    /// Activa la empresa y sus usuarios y le asigna el plan por <paramref name="days"/> días más:
    /// sin vencimiento o vencido, desde hoy; vigente (vence hoy o después), desde su vencimiento actual.
    /// </summary>
    private async Task<PlanResponse?> ActivatePlanAsync(int applicationId, ApplicationType type, int days)
    {
        var application = await dbContext.Applications
            .Include(a => a.Users)
            .FirstOrDefaultAsync(a => a.Id == applicationId);
        if (application is null)
            return null;

        var now = ArgentinaClock.Now;
        var expiresAt = application.PlanExpirationDate;
        var isCurrent = expiresAt.HasValue && expiresAt.Value.Date >= now.Date;

        // El inicio del plan solo cambia si empieza uno nuevo; al extender el mismo plan vigente se conserva.
        if (application.Plan != type || !isCurrent)
            application.PlanStartDate = now;

        application.Plan = type;
        application.PlanExpirationDate = isCurrent ? expiresAt!.Value.AddDays(days) : now.AddDays(days);
        application.Active = true;
        application.Guid ??= System.Guid.NewGuid().ToString();
        foreach (var user in application.Users)
            user.IsActive = true;

        // El período nuevo rige desde hoy, o desde el día siguiente al vencimiento si el plan seguía vigente.
        dbContext.Plans.Add(new Plan
        {
            CreatedAt = now,
            ApplicationId = application.Id,
            Type = type,
            StartDate = isCurrent ? expiresAt!.Value.AddDays(1) : now,
            ExpirationDate = application.PlanExpirationDate
        });

        // Un único SaveChanges: empresa, usuarios, plan e historial se guardan juntos o no se guarda nada.
        await dbContext.SaveChangesAsync();

        return ToResponse(application, now.Date);
    }

    /// <summary>Planes activados para la empresa, del más reciente al más antiguo.</summary>
    public async Task<List<PlanHistoryResponse>> GetHistoryAsync(int applicationId)
    {
        var today = ArgentinaClock.Now.Date;
        var rows = await dbContext.Plans
            .AsNoTracking()
            .Where(p => p.ApplicationId == applicationId)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .ToListAsync();

        return rows.Select(p => new PlanHistoryResponse
        {
            Id = p.Id,
            CreatedAt = p.CreatedAt,
            Plan = ApplicationTypeNames.Display(p.Type),
            StartDate = p.StartDate,
            ExpirationDate = p.ExpirationDate,
            DaysLeft = p.ExpirationDate.HasValue ? (p.ExpirationDate.Value.Date - today).Days : null
        }).ToList();
    }

    private static PlanResponse ToResponse(Domain.Entities.Application a, DateTime today) => new()
    {
        ApplicationId = a.Id,
        ApplicationName = a.Name,
        Address = a.Address,
        Locality = a.Locality,
        WhatsApp = a.WhatsApp,
        Active = a.Active,
        Plan = ApplicationTypeNames.Display(a.Plan),
        StartDate = a.PlanStartDate,
        ExpirationDate = a.PlanExpirationDate,
        DaysLeft = a.PlanExpirationDate.HasValue ? (a.PlanExpirationDate.Value.Date - today).Days : null
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
