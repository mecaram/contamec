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
        ValidateExpense(request.AccountId, request.Amount, request.Detail);
        await EnsureAccountBelongsToApplicationAsync(request.AccountId!.Value, applicationId);
        var openClosureId = await GetOpenClosureIdAsync(applicationId);
        if (!openClosureId.HasValue)
            throw new ArgumentException("No existe un cierre abierto (IsClosed = 0).");

        var expense = new Expense
        {
            EmissionDate = request.EmissionDate,
            AccountId = request.AccountId!.Value,
            Detail = request.Detail?.Trim() ?? string.Empty,
            Amount = request.Amount,
            CreatedAt = DateTime.Now,
            CreatedByUserId = createdByUserId,
            ClosureId = openClosureId.Value,
            ApplicationId = applicationId
        };

        _dbContext.Expenses.Add(expense);
        await _dbContext.SaveChangesAsync();

        var created = await GetByIdAsync(expense.Id, applicationId);
        return created ?? throw new InvalidOperationException("No se pudo leer el egreso recién creado.");
    }

    public async Task<bool> UpdateAsync(int id, ExpenseUpdateRequest request, int applicationId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateExpense(request.AccountId, request.Amount, request.Detail);
        await EnsureAccountBelongsToApplicationAsync(request.AccountId!.Value, applicationId);

        var expense = await _dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.ApplicationId == applicationId);
        if (expense is null)
            return false;
        if (!await CanModifyExpenseAsync(expense.ClosureId))
            throw new ArgumentException("Solo se puede modificar un egreso con cierre abierto (IsClosed = 0).");

        expense.EmissionDate = request.EmissionDate;
        expense.AccountId = request.AccountId!.Value;
        expense.Detail = request.Detail?.Trim() ?? string.Empty;
        expense.Amount = request.Amount;

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

    // Una cuenta de otra aplicación se trata como inexistente.
    private async Task EnsureAccountBelongsToApplicationAsync(int accountId, int applicationId)
    {
        var exists = await _dbContext.Accounts
            .AnyAsync(a => a.Id == accountId && a.ApplicationId == applicationId);
        if (!exists)
            throw new ArgumentException("La cuenta seleccionada no existe.");
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
            CreatedByUserId = expense.CreatedByUserId,
            CreatedAt = expense.CreatedAt
        };
    }

    private static void ValidateExpense(int? accountId, decimal? amount, string? detail)
    {
        if (!accountId.HasValue)
            throw new ArgumentException("AccountId es obligatorio.");

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
