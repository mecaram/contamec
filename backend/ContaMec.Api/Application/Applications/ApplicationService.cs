using ContaMec.Api.Application.Applications.Dto;
using ContaMec.Api.Domain.Entities;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
// Alias: "Application" choca con el namespace ContaMec.Api.Application.
using ApplicationEntity = ContaMec.Api.Domain.Entities.Application;

namespace ContaMec.Api.Application.Applications;

public class ApplicationService(ContaMecDbContext dbContext) : IApplicationService
{
    private readonly ContaMecDbContext _dbContext = dbContext;

    public async Task<List<ApplicationResponse>> SearchAsync(ApplicationSearchRequest request)
    {
        request ??= new ApplicationSearchRequest();

        var query = _dbContext.Applications.AsNoTracking().AsQueryable();

        if (request.Id.HasValue)
            query = query.Where(a => a.Id == request.Id.Value);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(a => a.Name.ToLower().Contains(name.ToLower()));
        }

        if (request.Active.HasValue)
            query = query.Where(a => a.Active == request.Active.Value);

        return await query
            .OrderBy(a => a.Name)
            .ThenBy(a => a.Id)
            .Select(MapToResponse())
            .ToListAsync();
    }

    public async Task<ApplicationResponse?> GetByIdAsync(int id)
    {
        return await _dbContext.Applications
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(MapToResponse())
            .FirstOrDefaultAsync();
    }

    public async Task<ApplicationResponse> CreateAsync(ApplicationCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ValidateApplicationAsync(request.Name, null);

        var application = new ApplicationEntity
        {
            Name = request.Name!.Trim(),
            Active = request.Active,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Applications.Add(application);
        // Toda empresa necesita un cierre abierto para registrar ingresos y egresos; se crea en la misma transacción.
        _dbContext.Closures.Add(new Closure
        {
            OpenDate = DateTime.Now,
            IsClosed = false,
            Application = application
        });
        await _dbContext.SaveChangesAsync();

        var created = await GetByIdAsync(application.Id);
        return created ?? throw new InvalidOperationException("No se pudo leer la aplicación recién creada.");
    }

    public async Task<bool> UpdateAsync(int id, ApplicationUpdateRequest request, int currentApplicationId)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ValidateApplicationAsync(request.Name, id);

        var application = await _dbContext.Applications.FirstOrDefaultAsync(a => a.Id == id);
        if (application is null)
            return false;

        // Desactivar la propia aplicación dejaría a todos sus usuarios (incluido el actual) sin acceso.
        if (id == currentApplicationId && !request.Active)
            throw new ArgumentException("No puede desactivar la aplicación con la que inició sesión.");

        application.Name = request.Name!.Trim();
        application.Active = request.Active;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int currentApplicationId)
    {
        var application = await _dbContext.Applications.FirstOrDefaultAsync(a => a.Id == id);
        if (application is null)
            return false;

        if (id == currentApplicationId)
            throw new ArgumentException("No puede eliminar la aplicación con la que inició sesión.");

        // Las claves foráneas hacia Applications no borran en cascada.
        var hasData =
            await _dbContext.Users.AnyAsync(x => x.ApplicationId == id)
            || await _dbContext.Accounts.AnyAsync(x => x.ApplicationId == id)
            || await _dbContext.PaymentAccounts.AnyAsync(x => x.ApplicationId == id)
            || await _dbContext.Closures.AnyAsync(x => x.ApplicationId == id)
            || await _dbContext.ClosureBalances.AnyAsync(x => x.ApplicationId == id)
            || await _dbContext.Incomes.AnyAsync(x => x.ApplicationId == id)
            || await _dbContext.Expenses.AnyAsync(x => x.ApplicationId == id);
        if (hasData)
            throw new ArgumentException("No se puede eliminar la aplicación porque tiene datos asociados.");

        _dbContext.Applications.Remove(application);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private static Expression<Func<ApplicationEntity, ApplicationResponse>> MapToResponse()
    {
        return application => new ApplicationResponse
        {
            Id = application.Id,
            Name = application.Name,
            Active = application.Active,
            CreatedAt = application.CreatedAt
        };
    }

    private async Task ValidateApplicationAsync(string? name, int? currentId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name es obligatorio.");
        if (name.Trim().Length > 100)
            throw new ArgumentException("Name no puede superar 100 caracteres.");

        var normalizedName = name.Trim();
        var exists = await _dbContext.Applications
            .AnyAsync(a => a.Name.ToLower() == normalizedName.ToLower() && (!currentId.HasValue || a.Id != currentId.Value));
        if (exists)
            throw new ArgumentException("Ya existe una aplicación con ese nombre.");
    }
}
