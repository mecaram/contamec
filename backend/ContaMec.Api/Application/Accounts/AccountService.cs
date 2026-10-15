using ContaMec.Api.Application.Accounts.Dto;
using ContaMec.Api.Domain.Entities;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ContaMec.Api.Application.Accounts;

public class AccountService(ContaMecDbContext dbContext) : IAccountService
{
    private readonly ContaMecDbContext _dbContext = dbContext;

    public async Task<List<AccountResponse>> SearchAsync(AccountSearchRequest request)
    {
        request ??= new AccountSearchRequest();

        var query = _dbContext.Accounts.AsNoTracking().AsQueryable();

        if (request.Id.HasValue)
            query = query.Where(a => a.Id == request.Id.Value);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(a => a.Name != null && a.Name.ToLower().Contains(name.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = request.Type.Trim();
            var matchingTypes = Enum.GetValues<AccountType>()
                .Where(t => t.ToString().Contains(type, StringComparison.OrdinalIgnoreCase))
                .Cast<AccountType?>()
                .ToList();
            query = query.Where(a => matchingTypes.Contains(a.Type));
        }

        return await query
            .OrderBy(a => a.Name)
            .ThenBy(a => a.Id)
            .Select(MapToResponse())
            .ToListAsync();
    }

    public async Task<AccountResponse?> GetByIdAsync(int id)
    {
        return await _dbContext.Accounts
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(MapToResponse())
            .FirstOrDefaultAsync();
    }

    public async Task<AccountResponse> CreateAsync(AccountCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateAccount(request.Name, request.Type);

        var account = new Account
        {
            Name = request.Name?.Trim(),
            Type = ParseType(request.Type)
        };

        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();

        var created = await GetByIdAsync(account.Id);
        return created ?? throw new InvalidOperationException("No se pudo leer la cuenta recién creada.");
    }

    public async Task<bool> UpdateAsync(int id, AccountUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateAccount(request.Name, request.Type);

        var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == id);
        if (account is null)
            return false;

        account.Name = request.Name?.Trim();
        account.Type = ParseType(request.Type);

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var account = await _dbContext.Accounts
            .Include(a => a.Incomes)
            .Include(a => a.Expenses)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (account is null)
            return false;

        if ((account.Incomes?.Count ?? 0) > 0 || (account.Expenses?.Count ?? 0) > 0)
            throw new ArgumentException("No se puede eliminar la cuenta porque tiene movimientos asociados.");

        _dbContext.Accounts.Remove(account);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private static Expression<Func<Account, AccountResponse>> MapToResponse()
    {
        return account => new AccountResponse
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type == AccountType.Ingresos ? nameof(AccountType.Ingresos)
                : account.Type == AccountType.Egresos ? nameof(AccountType.Egresos)
                : null
        };
    }

    private static void ValidateAccount(string? name, string? type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name es obligatorio.");
        if (name.Trim().Length > 50)
            throw new ArgumentException("Name no puede superar 50 caracteres.");

        ParseType(type);
    }

    // El frontend envía "Ingreso"/"Egreso"; en la base es el enum AccountTypes (Ingresos, Egresos).
    private static AccountType ParseType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type es obligatorio.");

        var value = type.Trim();
        if (value.Contains("ingreso", StringComparison.OrdinalIgnoreCase))
            return AccountType.Ingresos;
        if (value.Contains("egreso", StringComparison.OrdinalIgnoreCase) || value.Contains("gasto", StringComparison.OrdinalIgnoreCase))
            return AccountType.Egresos;

        throw new ArgumentException("Type debe ser Ingreso o Egreso.");
    }
}
