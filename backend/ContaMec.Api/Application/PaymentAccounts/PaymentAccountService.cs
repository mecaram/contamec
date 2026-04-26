using ContaMec.Api.Application.PaymentAccounts.Dto;
using ContaMec.Api.Domain.Entities;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ContaMec.Api.Application.PaymentAccounts;

public class PaymentAccountService(ContaMecDbContext dbContext) : IPaymentAccountService
{
    private readonly ContaMecDbContext _dbContext = dbContext;

    public async Task<List<PaymentAccountResponse>> SearchAsync(PaymentAccountSearchRequest request)
    {
        request ??= new PaymentAccountSearchRequest();

        var query = _dbContext.PaymentAccounts.AsNoTracking().AsQueryable();

        if (request.Id.HasValue)
            query = query.Where(a => a.Id == request.Id.Value);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(a => a.Name != null && a.Name.Contains(name));
        }

        return await query
            .OrderBy(a => a.Name)
            .ThenBy(a => a.Id)
            .Select(MapToResponse())
            .ToListAsync();
    }

    public async Task<PaymentAccountResponse?> GetByIdAsync(int id)
    {
        return await _dbContext.PaymentAccounts
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(MapToResponse())
            .FirstOrDefaultAsync();
    }

    public async Task<PaymentAccountResponse> CreateAsync(PaymentAccountCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidatePaymentAccount(request.Name);

        var account = new PaymentAccount
        {
            Name = request.Name?.Trim() ?? string.Empty
        };

        _dbContext.PaymentAccounts.Add(account);
        await _dbContext.SaveChangesAsync();

        var created = await GetByIdAsync(account.Id);
        return created ?? throw new InvalidOperationException("No se pudo leer la cuenta de pago recién creada.");
    }

    public async Task<bool> UpdateAsync(int id, PaymentAccountUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidatePaymentAccount(request.Name);

        var account = await _dbContext.PaymentAccounts.FirstOrDefaultAsync(a => a.Id == id);
        if (account is null)
            return false;

        account.Name = request.Name?.Trim() ?? string.Empty;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var account = await _dbContext.PaymentAccounts
            .Include(a => a.ClosureBalances)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (account is null)
            return false;

        if ((account.ClosureBalances?.Count ?? 0) > 0)
            throw new ArgumentException("No se puede eliminar la cuenta de pago porque tiene movimientos asociados.");

        _dbContext.PaymentAccounts.Remove(account);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private static Expression<Func<PaymentAccount, PaymentAccountResponse>> MapToResponse()
    {
        return account => new PaymentAccountResponse
        {
            Id = account.Id,
            Name = account.Name
        };
    }

    private static void ValidatePaymentAccount(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name es obligatorio.");
        if (name.Trim().Length > 50)
            throw new ArgumentException("Name no puede superar 50 caracteres.");
    }
}
