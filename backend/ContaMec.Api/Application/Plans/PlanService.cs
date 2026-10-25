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
            .Select(a => new { a.Id, a.Name, a.Address, a.Locality, a.WhatsApp, a.Active, a.Type, a.ApplicationTypeDate, a.ApplicationExpirationDate })
            .ToListAsync();

        return rows.Select(a => new PlanResponse
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
        }).ToList();
    }

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
