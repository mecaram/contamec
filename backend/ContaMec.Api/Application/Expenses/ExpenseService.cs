using ContaMec.Api.Application.Expenses.Dto;
using ContaMec.Api.Domain.Entities;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;

namespace ContaMec.Api.Application.Expenses;

public class ExpenseService(ContaMecDbContext dbContext) : IExpenseService
{
    private readonly ContaMecDbContext _dbContext = dbContext;

    public async Task<List<ExpenseResponse>> SearchAsync(ExpenseSearchRequest request, int applicationId)
    {
        request ??= new ExpenseSearchRequest();

        var query = _dbContext.Expenses
            .AsNoTracking()
            .Include(e => e.Account)
            .Where(e => e.ApplicationId == applicationId);

        if (request.DateFrom.HasValue)
            query = query.Where(e => e.EmissionDate >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(e => e.EmissionDate <= request.DateTo.Value);

        if (request.ClosureId.HasValue)
            query = query.Where(e => e.ClosureId == request.ClosureId.Value);

        if (request.AccountId.HasValue)
            query = query.Where(e => e.AccountId == request.AccountId.Value);

        if (request.PaymentAccountId.HasValue)
            query = query.Where(e => e.PaymentAccountId == request.PaymentAccountId.Value);

        if (request.SubAccountId.HasValue)
            query = query.Where(e => e.SubAccountId == request.SubAccountId.Value);

        if (request.AmountFrom.HasValue)
            query = query.Where(e => e.Amount.HasValue && e.Amount.Value >= request.AmountFrom.Value);

        if (request.AmountTo.HasValue)
            query = query.Where(e => e.Amount.HasValue && e.Amount.Value <= request.AmountTo.Value);

        if (!string.IsNullOrWhiteSpace(request.Detail))
        {
            var detail = request.Detail.Trim();
            query = query.Where(e => e.Detail != null && e.Detail.ToLower().Contains(detail.ToLower()));
        }

        return await query
            .OrderByDescending(e => e.EmissionDate)
            .ThenByDescending(e => e.Id)
            .Select(MapToResponse())
            .ToListAsync();
    }

    public async Task<ExpenseResponse?> GetByIdAsync(int id, int applicationId)
    {
        return await _dbContext.Expenses
            .AsNoTracking()
            .Include(e => e.Account)
            .Where(e => e.Id == id && e.ApplicationId == applicationId)
            .Select(MapToResponse())
            .FirstOrDefaultAsync();
    }

    public async Task<ExpenseResponse> CreateAsync(ExpenseCreateRequest request, int createdByUserId, int applicationId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateExpense(request.AccountId, request.Amount, request.Detail, request.PaymentAccountId);
        var subAccountRequires = await GetAccountSubAccountRequiresAsync(request.AccountId!.Value, applicationId);
        await EnsurePaymentAccountBelongsToApplicationAsync(request.PaymentAccountId!.Value, applicationId);
        var subAccountId = await ResolveSubAccountIdAsync(request.AccountId!.Value, subAccountRequires, request.SubAccountId, null);
        var occurrenceType = ParseOccurrenceType(request.OccurrenceType);
        var openClosureId = await GetOpenClosureIdAsync(applicationId);
        if (!openClosureId.HasValue)
            throw new ArgumentException("No existe un cierre abierto (IsClosed = 0).");

        var expense = new Expense
        {
            EmissionDate = request.EmissionDate,
            AccountId = request.AccountId!.Value,
            Detail = request.Detail?.Trim() ?? string.Empty,
            Amount = request.Amount,
            PaymentAccountId = request.PaymentAccountId!.Value,
            SubAccountId = subAccountId,
            OccurrenceType = occurrenceType,
            CreatedAt = ArgentinaClock.Now,
            CreatedByUserId = createdByUserId,
            ClosureId = openClosureId.Value,
            ApplicationId = applicationId
        };

        _dbContext.Expenses.Add(expense);
        await _dbContext.SaveChangesAsync();

        var created = await GetByIdAsync(expense.Id, applicationId);
        return created ?? throw new InvalidOperationException("No se pudo leer el egreso recién creado.");
    }

    // Cuenta de pago del último egreso que registró el usuario en la aplicación, para proponerla en el alta.
    public async Task<int?> GetLastPaymentAccountIdAsync(int userId, int applicationId)
    {
        return await _dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.ApplicationId == applicationId && e.CreatedByUserId == userId && e.PaymentAccountId != null)
            .OrderByDescending(e => e.Id)
            .Select(e => e.PaymentAccountId)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(int id, ExpenseUpdateRequest request, int applicationId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateExpense(request.AccountId, request.Amount, request.Detail, request.PaymentAccountId);
        var subAccountRequires = await GetAccountSubAccountRequiresAsync(request.AccountId!.Value, applicationId);
        await EnsurePaymentAccountBelongsToApplicationAsync(request.PaymentAccountId!.Value, applicationId);

        var expense = await _dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.ApplicationId == applicationId);
        if (expense is null)
            return false;
        if (!await CanModifyExpenseAsync(expense.ClosureId))
            throw new ArgumentException("Solo se puede modificar un egreso con cierre abierto (IsClosed = 0).");

        var subAccountId = await ResolveSubAccountIdAsync(request.AccountId!.Value, subAccountRequires, request.SubAccountId, expense.SubAccountId);
        var occurrenceType = ParseOccurrenceType(request.OccurrenceType);

        expense.EmissionDate = request.EmissionDate;
        expense.AccountId = request.AccountId!.Value;
        expense.Detail = request.Detail?.Trim() ?? string.Empty;
        expense.Amount = request.Amount;
        expense.PaymentAccountId = request.PaymentAccountId!.Value;
        expense.SubAccountId = subAccountId;
        expense.OccurrenceType = occurrenceType;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int applicationId)
    {
        var expense = await _dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.ApplicationId == applicationId);
        if (expense is null)
            return false;
        if (!await CanModifyExpenseAsync(expense.ClosureId))
            throw new ArgumentException("Solo se puede eliminar un egreso con cierre abierto (IsClosed = 0).");

        _dbContext.Expenses.Remove(expense);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    // Una cuenta de otra aplicación se trata como inexistente. Devuelve si la cuenta requiere subcuentas.
    private async Task<bool> GetAccountSubAccountRequiresAsync(int accountId, int applicationId)
    {
        var account = await _dbContext.Accounts
            .AsNoTracking()
            .Where(a => a.Id == accountId && a.ApplicationId == applicationId)
            .Select(a => new { a.SubAccountRequires })
            .FirstOrDefaultAsync();
        if (account is null)
            throw new ArgumentException("La cuenta seleccionada no existe.");
        return account.SubAccountRequires == true;
    }

    // Si la cuenta requiere subcuentas, la subcuenta es obligatoria, debe ser de esa cuenta y estar activa
    // (se acepta la que el egreso ya tenía aunque se haya desactivado). Si no las requiere, no se guarda subcuenta.
    private async Task<int?> ResolveSubAccountIdAsync(int accountId, bool subAccountRequires, int? subAccountId, int? currentSubAccountId)
    {
        if (!subAccountRequires)
            return null;
        if (!subAccountId.HasValue)
            throw new ArgumentException("Debe seleccionar una subcuenta.");

        var subAccount = await _dbContext.SubAccounts
            .AsNoTracking()
            .Where(s => s.Id == subAccountId.Value && s.AccountId == accountId)
            .Select(s => new { s.Active })
            .FirstOrDefaultAsync();
        if (subAccount is null)
            throw new ArgumentException("La subcuenta seleccionada no pertenece a la cuenta.");
        if (subAccount.Active == false && subAccountId != currentSubAccountId)
            throw new ArgumentException("La subcuenta seleccionada está inactiva.");

        return subAccountId;
    }

    // Igual que la cuenta: una cuenta de pago de otra aplicación se trata como inexistente.
    private async Task EnsurePaymentAccountBelongsToApplicationAsync(int paymentAccountId, int applicationId)
    {
        var exists = await _dbContext.PaymentAccounts
            .AnyAsync(a => a.Id == paymentAccountId && a.ApplicationId == applicationId);
        if (!exists)
            throw new ArgumentException("La cuenta de pago seleccionada no existe.");
    }

    private static Expression<Func<Expense, ExpenseResponse>> MapToResponse()
    {
        return expense => new ExpenseResponse
        {
            Id = expense.Id,
            ClosureId = expense.ClosureId,
            EmissionDate = expense.EmissionDate,
            AccountId = expense.AccountId,
            AccountName = expense.Account != null ? expense.Account.Name : null,
            Detail = expense.Detail,
            Amount = expense.Amount,
            PaymentAccountId = expense.PaymentAccountId,
            PaymentAccountName = expense.PaymentAccount != null ? expense.PaymentAccount.Name : null,
            SubAccountId = expense.SubAccountId,
            SubAccountName = expense.SubAccount != null ? expense.SubAccount.Name : null,
            OccurrenceType = expense.OccurrenceType == OccurrenceType.Ordinaria ? nameof(OccurrenceType.Ordinaria)
                : expense.OccurrenceType == OccurrenceType.Extraordinaria ? nameof(OccurrenceType.Extraordinaria)
                : null,
            CreatedByUserId = expense.CreatedByUserId,
            CreatedAt = expense.CreatedAt
        };
    }

    // Etiquetas del enum OccurrenceTypes de la base: Ordinaria, Extraordinaria.
    private static OccurrenceType ParseOccurrenceType(string? occurrenceType)
    {
        if (string.IsNullOrWhiteSpace(occurrenceType))
            throw new ArgumentException("Debe seleccionar una ocurrencia.");
        if (Enum.TryParse<OccurrenceType>(occurrenceType.Trim(), ignoreCase: true, out var value) && Enum.IsDefined(value))
            return value;

        throw new ArgumentException("OccurrenceType debe ser Ordinaria o Extraordinaria.");
    }

    private static void ValidateExpense(int? accountId, decimal? amount, string? detail, int? paymentAccountId)
    {
        if (!accountId.HasValue)
            throw new ArgumentException("AccountId es obligatorio.");

        if (!paymentAccountId.HasValue)
            throw new ArgumentException("Debe seleccionar una cuenta de pago.");

        if (!amount.HasValue || amount.Value <= 0)
            throw new ArgumentException("Amount debe ser mayor a 0.");

        if (!string.IsNullOrEmpty(detail) && detail.Length > 80)
            throw new ArgumentException("Detail no puede superar 80 caracteres.");
    }

    private async Task<int?> GetOpenClosureIdAsync(int applicationId)
    {
        var tableNames = await ResolveClosureTableNamesAsync();
        if (tableNames.Count == 0)
            return null;

        foreach (var tableName in tableNames)
        {
            var closureId = await GetOpenClosureIdFromTableAsync(tableName, applicationId);
            if (closureId.HasValue)
                return closureId;
        }

        return null;
    }

    private async Task<bool> CanModifyExpenseAsync(int? closureId)
    {
        if (!closureId.HasValue)
            return false;

        var tableNames = await ResolveClosureTableNamesAsync();
        if (tableNames.Count == 0)
            return false;

        var foundAny = false;
        foreach (var tableName in tableNames)
        {
            var isClosed = await GetIsClosedByClosureIdAsync(tableName, closureId.Value);
            if (!isClosed.HasValue)
                continue;

            foundAny = true;
            if (isClosed.Value)
                return false;
        }

        return foundAny;
    }

    private async Task<int?> GetOpenClosureIdFromTableAsync(string tableName, int applicationId)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $@"SELECT ""Id"" FROM ""{tableName}"" WHERE ""isClosed"" = FALSE AND ""ApplicationId"" = @applicationId ORDER BY ""Id"" DESC LIMIT 1";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@applicationId";
            parameter.Value = applicationId;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync();

            if (result is null || result == DBNull.Value)
                return null;

            return Convert.ToInt32(result);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private async Task<bool?> GetIsClosedByClosureIdAsync(string tableName, int closureId)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $@"SELECT ""isClosed"" FROM ""{tableName}"" WHERE ""Id"" = @closureId LIMIT 1";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@closureId";
            parameter.Value = closureId;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync();
            if (result is null || result == DBNull.Value)
                return null;

            return Convert.ToInt32(result) != 0;
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private async Task<List<string>> ResolveClosureTableNamesAsync()
    {
        const string sql = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = current_schema()
              AND table_name IN ('Closures', 'Cierres')
            ORDER BY CASE WHEN table_name = 'Closures' THEN 0 ELSE 1 END
            """;

        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var tables = new List<string>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                {
                    var tableName = reader.GetString(0);
                    if (!string.IsNullOrWhiteSpace(tableName))
                        tables.Add(tableName);
                }
            }

            return tables;
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }
}
