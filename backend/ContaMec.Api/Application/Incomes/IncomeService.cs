using ContaMec.Api.Application.Incomes.Dto;
using ContaMec.Api.Domain.Entities;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;

namespace ContaMec.Api.Application.Incomes;

public class IncomeService(ContaMecDbContext dbContext) : IIncomeService
{
    private readonly ContaMecDbContext _dbContext = dbContext;

    public async Task<List<IncomeResponse>> SearchAsync(IncomeSearchRequest request)
    {
        request ??= new IncomeSearchRequest();

        var query = _dbContext.Incomes
            .AsNoTracking()
            .Include(i => i.Account)
            .AsQueryable();

        if (request.DateFrom.HasValue)
            query = query.Where(i => i.EmissionDate >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(i => i.EmissionDate <= request.DateTo.Value);

        if (request.ClosureId.HasValue)
            query = query.Where(i => i.ClosureId == request.ClosureId.Value);

        if (request.AccountId.HasValue)
            query = query.Where(i => i.AccountId == request.AccountId.Value);

        if (request.AmountFrom.HasValue)
            query = query.Where(i => i.Amount.HasValue && i.Amount.Value >= request.AmountFrom.Value);

        if (request.AmountTo.HasValue)
            query = query.Where(i => i.Amount.HasValue && i.Amount.Value <= request.AmountTo.Value);

        if (!string.IsNullOrWhiteSpace(request.Detail))
        {
            var detail = request.Detail.Trim();
            query = query.Where(i => i.Detail != null && i.Detail.Contains(detail));
        }

        return await query
            .OrderByDescending(i => i.EmissionDate)
            .ThenByDescending(i => i.Id)
            .Select(MapToResponse())
            .ToListAsync();
    }

    public async Task<IncomeResponse?> GetByIdAsync(int id)
    {
        return await _dbContext.Incomes
            .AsNoTracking()
            .Include(i => i.Account)
            .Where(i => i.Id == id)
            .Select(MapToResponse())
            .FirstOrDefaultAsync();
    }

    public async Task<IncomeResponse> CreateAsync(IncomeCreateRequest request, int createdByUserId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateIncome(request.Amount, request.Detail);
        var openClosureId = await GetOpenClosureIdAsync();
        if (!openClosureId.HasValue)
            throw new ArgumentException("No existe un cierre abierto (IsClosed = 0).");

        var income = new Income
        {
            EmissionDate = request.EmissionDate,
            AccountId = request.AccountId,
            Detail = request.Detail?.Trim() ?? string.Empty,
            Amount = request.Amount,
            CreatedAt = DateTime.Now,
            CreatedByUserId = createdByUserId,
            ClosureId = openClosureId.Value
        };

        _dbContext.Incomes.Add(income);
        await _dbContext.SaveChangesAsync();

        var created = await GetByIdAsync(income.Id);
        return created ?? throw new InvalidOperationException("No se pudo leer el ingreso recién creado.");
    }

    public async Task<bool> UpdateAsync(int id, IncomeUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateIncome(request.Amount, request.Detail);

        var income = await _dbContext.Incomes.FirstOrDefaultAsync(i => i.Id == id);
        if (income is null)
            return false;
        if (!await CanModifyIncomeAsync(income.ClosureId))
            throw new ArgumentException("Solo se puede modificar un ingreso con cierre abierto (IsClosed = 0).");

        income.EmissionDate = request.EmissionDate;
        income.AccountId = request.AccountId;
        income.Detail = request.Detail?.Trim() ?? string.Empty;
        income.Amount = request.Amount;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var income = await _dbContext.Incomes.FirstOrDefaultAsync(i => i.Id == id);
        if (income is null)
            return false;
        if (!await CanModifyIncomeAsync(income.ClosureId))
            throw new ArgumentException("Solo se puede eliminar un ingreso con cierre abierto (IsClosed = 0).");

        _dbContext.Incomes.Remove(income);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private static Expression<Func<Income, IncomeResponse>> MapToResponse()
    {
        return income => new IncomeResponse
        {
            Id = income.Id,
            ClosureId = income.ClosureId,
            EmissionDate = income.EmissionDate,
            AccountId = income.AccountId,
            AccountName = income.Account != null ? income.Account.Name : null,
            Detail = income.Detail,
            Amount = income.Amount,
            CreatedByUserId = income.CreatedByUserId,
            CreatedAt = income.CreatedAt
        };
    }

    private static void ValidateIncome(decimal? amount, string? detail)
    {
        if (!amount.HasValue || amount.Value <= 0)
            throw new ArgumentException("Amount debe ser mayor a 0.");

        if (!string.IsNullOrEmpty(detail) && detail.Length > 80)
            throw new ArgumentException("Detail no puede superar 80 caracteres.");
    }

    private async Task<int?> GetOpenClosureIdAsync()
    {
        var tableNames = await ResolveClosureTableNamesAsync();
        if (tableNames.Count == 0)
            return null;

        foreach (var tableName in tableNames)
        {
            var closureId = await GetOpenClosureIdFromTableAsync(tableName);
            if (closureId.HasValue)
                return closureId;
        }

        return null;
    }

    private async Task<bool> CanModifyIncomeAsync(int? closureId)
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

    private async Task<int?> GetOpenClosureIdFromTableAsync(string tableName)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT TOP (1) [Id] FROM [{tableName}] WHERE [IsClosed] = 0 ORDER BY [Id] DESC";
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
            command.CommandText = $"SELECT TOP (1) [IsClosed] FROM [{tableName}] WHERE [Id] = @closureId";

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
            SELECT [name]
            FROM sys.tables
            WHERE [name] IN ('Closures', 'Cierres')
            ORDER BY CASE WHEN [name] = 'Closures' THEN 0 ELSE 1 END
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
