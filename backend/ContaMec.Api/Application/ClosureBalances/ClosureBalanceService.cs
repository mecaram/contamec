using ContaMec.Api.Application.ClosureBalances.Dto;
using ContaMec.Api.Domain.Entities;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ContaMec.Api.Application.ClosureBalances;

public class ClosureBalanceService(ContaMecDbContext dbContext) : IClosureBalanceService
{
    private readonly ContaMecDbContext _dbContext = dbContext;

    public async Task<List<ClosureBalanceResponse>> GetByClosureIdAsync(int closureId)
    {
        if (closureId <= 0)
            throw new ArgumentException("ClosureId inválido.");

        await EnsureClosureExistsAsync(closureId);

        var paymentAccounts = await _dbContext.PaymentAccounts
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        var existing = await _dbContext.ClosureBalances
            .AsNoTracking()
            .Where(x => x.ClosureId == closureId)
            .ToListAsync();

        var existingByPaymentAccount = existing
            .GroupBy(x => x.PaymentAccountId)
            .ToDictionary(g => g.Key, g => g.OrderBy(v => v.Id).First());

        return paymentAccounts
            .Select(account =>
            {
                var hasValue = existingByPaymentAccount.TryGetValue(account.Id, out var balance);
                return new ClosureBalanceResponse
                {
                    Id = hasValue ? balance!.Id : null,
                    ClosureId = closureId,
                    PaymentAccountId = account.Id,
                    PaymentAccountName = account.Name ?? string.Empty,
                    Amount = hasValue ? (balance!.Amount ?? 0m) : 0m
                };
            })
            .ToList();
    }

    public async Task<List<ClosureBalanceResponse>> SaveBulkAsync(int closureId, List<ClosureBalanceSaveRequest> items)
    {
        if (closureId <= 0)
            throw new ArgumentException("ClosureId inválido.");
        if (items is null || items.Count == 0)
            throw new ArgumentException("Debe enviar al menos un registro.");

        await EnsureClosureExistsAsync(closureId);

        var paymentAccountIds = await _dbContext.PaymentAccounts
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync();

        if (items.Any(x => x.ClosureId != closureId))
            throw new ArgumentException("Todos los registros deben pertenecer al mismo cierre.");
        if (items.Any(x => !x.Amount.HasValue))
            throw new ArgumentException("El monto no puede ser nulo.");
        if (items.Any(x => x.Amount < 0))
            throw new ArgumentException("El monto no puede ser negativo.");

        var duplicatedIds = items
            .GroupBy(x => x.PaymentAccountId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicatedIds.Count > 0)
            throw new ArgumentException("No se permiten cuentas de pago duplicadas.");

        var requestIds = items.Select(x => x.PaymentAccountId).OrderBy(x => x).ToList();
        var expectedIds = paymentAccountIds.OrderBy(x => x).ToList();
        if (requestIds.Count != expectedIds.Count || !requestIds.SequenceEqual(expectedIds))
            throw new ArgumentException("La lista debe contener exactamente una fila por cada cuenta de pago.");

        var existing = await _dbContext.ClosureBalances
            .Where(x => x.ClosureId == closureId)
            .ToListAsync();

        var existingByPaymentAccount = existing
            .GroupBy(x => x.PaymentAccountId)
            .ToDictionary(g => g.Key, g => g.OrderBy(v => v.Id).First());

        foreach (var item in items)
        {
            if (existingByPaymentAccount.TryGetValue(item.PaymentAccountId, out var current))
            {
                current.Amount = item.Amount ?? 0m;
            }
            else
            {
                _dbContext.ClosureBalances.Add(new ClosureBalance
                {
                    ClosureId = closureId,
                    PaymentAccountId = item.PaymentAccountId,
                    Amount = item.Amount ?? 0m
                });
            }
        }

        await _dbContext.SaveChangesAsync();
        return await GetByClosureIdAsync(closureId);
    }

    private async Task EnsureClosureExistsAsync(int closureId)
    {
        var closureTableName = await ResolveTableNameAsync("Closures", "Cierres");
        if (closureTableName is null)
            throw new ArgumentException("No se encontró la tabla de cierres en la base de datos.");

        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT TOP 1 1 FROM [{closureTableName}] WHERE [Id] = @closureId";
            AddNullableParameter(command, "@closureId", DbType.Int32, closureId);
            var exists = await command.ExecuteScalarAsync();
            if (exists is null)
                throw new ArgumentException("El cierre indicado no existe.");
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private async Task<string?> ResolveTableNameAsync(string preferredName, string fallbackName)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            foreach (var candidate in new[] { preferredName, fallbackName })
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT TOP 1 1 FROM sys.tables WHERE [name] = @tableName";
                AddNullableParameter(command, "@tableName", DbType.String, candidate);
                var exists = await command.ExecuteScalarAsync();
                if (exists is not null)
                    return candidate;
            }

            return null;
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private static void AddNullableParameter(IDbCommand command, string name, DbType dbType, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = dbType;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
