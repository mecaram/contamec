using ContaMec.Api.Application.SubAccounts.Dto;
using ContaMec.Api.Domain.Entities;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ContaMec.Api.Application.SubAccounts;

public class SubAccountService(ContaMecDbContext dbContext) : ISubAccountService
{
    private readonly ContaMecDbContext _dbContext = dbContext;

    public async Task<List<SubAccountResponse>> SearchAsync(SubAccountSearchRequest request, int applicationId)
    {
        request ??= new SubAccountSearchRequest();

        var query = QueryForApplication(applicationId);

        if (request.Id.HasValue)
            query = query.Where(s => s.Id == request.Id.Value);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(s => s.Name != null && s.Name.ToLower().Contains(name.ToLower()));
        }

        if (request.AccountId.HasValue)
            query = query.Where(s => s.AccountId == request.AccountId.Value);

        return await query
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Id)
            .Select(MapToResponse())
            .ToListAsync();
    }

    public async Task<SubAccountResponse?> GetByIdAsync(int id, int applicationId)
    {
        // Una subcuenta de otra aplicación se trata como inexistente.
        return await QueryForApplication(applicationId)
            .Where(s => s.Id == id)
            .Select(MapToResponse())
            .FirstOrDefaultAsync();
    }

    public async Task<SubAccountResponse> CreateAsync(SubAccountCreateRequest request, int applicationId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateSubAccount(request.Name, request.AccountId);
        var behaviorType = ParseBehaviorType(request.BehaviorType);
        await EnsureAccountBelongsToApplicationAsync(request.AccountId!.Value, applicationId);

        var subAccount = new SubAccount
        {
            Name = request.Name?.Trim(),
            AccountId = request.AccountId,
            BehaviorType = behaviorType,
            Active = request.Active
        };

        _dbContext.SubAccounts.Add(subAccount);
        await _dbContext.SaveChangesAsync();

        var created = await GetByIdAsync(subAccount.Id, applicationId);
        return created ?? throw new InvalidOperationException("No se pudo leer la subcuenta recién creada.");
    }

    public async Task<bool> UpdateAsync(int id, SubAccountUpdateRequest request, int applicationId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateSubAccount(request.Name, request.AccountId);
        var behaviorType = ParseBehaviorType(request.BehaviorType);

        var subAccount = await _dbContext.SubAccounts
            .FirstOrDefaultAsync(s => s.Id == id && s.Account != null && s.Account.ApplicationId == applicationId);
        if (subAccount is null)
            return false;

        await EnsureAccountBelongsToApplicationAsync(request.AccountId!.Value, applicationId);

        subAccount.Name = request.Name?.Trim();
        subAccount.AccountId = request.AccountId;
        subAccount.BehaviorType = behaviorType;
        subAccount.Active = request.Active;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int applicationId)
    {
        var subAccount = await _dbContext.SubAccounts
            .Include(s => s.Incomes)
            .Include(s => s.Expenses)
            .FirstOrDefaultAsync(s => s.Id == id && s.Account != null && s.Account.ApplicationId == applicationId);
        if (subAccount is null)
            return false;

        if ((subAccount.Incomes?.Count ?? 0) > 0 || (subAccount.Expenses?.Count ?? 0) > 0)
            throw new ArgumentException("No se puede eliminar la subcuenta porque tiene movimientos asociados.");

        _dbContext.SubAccounts.Remove(subAccount);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    // SubAccounts no tiene ApplicationId: la aplicación se toma de la cuenta a la que pertenece.
    private IQueryable<SubAccount> QueryForApplication(int applicationId)
    {
        return _dbContext.SubAccounts
            .AsNoTracking()
            .Where(s => s.Account != null && s.Account.ApplicationId == applicationId);
    }

    private async Task EnsureAccountBelongsToApplicationAsync(int accountId, int applicationId)
    {
        var exists = await _dbContext.Accounts.AnyAsync(a => a.Id == accountId && a.ApplicationId == applicationId);
        if (!exists)
            throw new ArgumentException("La cuenta seleccionada no existe.");
    }

    private static Expression<Func<SubAccount, SubAccountResponse>> MapToResponse()
    {
        return subAccount => new SubAccountResponse
        {
            Id = subAccount.Id,
            Name = subAccount.Name,
            AccountId = subAccount.AccountId,
            AccountName = subAccount.Account != null ? subAccount.Account.Name : null,
            BehaviorType = subAccount.BehaviorType == BehaviorType.Fijo ? nameof(BehaviorType.Fijo)
                : subAccount.BehaviorType == BehaviorType.Variable ? nameof(BehaviorType.Variable)
                : subAccount.BehaviorType == BehaviorType.Mixto ? nameof(BehaviorType.Mixto)
                : null,
            Active = subAccount.Active
        };
    }

    private static void ValidateSubAccount(string? name, int? accountId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name es obligatorio.");
        if (name.Trim().Length > 50)
            throw new ArgumentException("Name no puede superar 50 caracteres.");
        if (!accountId.HasValue)
            throw new ArgumentException("Debe seleccionar una cuenta.");
    }

    // Etiquetas del enum BehaviorTypes de la base: Fijo, Variable, Mixto.
    private static BehaviorType ParseBehaviorType(string? behaviorType)
    {
        if (string.IsNullOrWhiteSpace(behaviorType))
            throw new ArgumentException("Debe seleccionar un comportamiento.");
        if (Enum.TryParse<BehaviorType>(behaviorType.Trim(), ignoreCase: true, out var value) && Enum.IsDefined(value))
            return value;

        throw new ArgumentException("BehaviorType debe ser Fijo, Variable o Mixto.");
    }
}
