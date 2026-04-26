using ContaMec.Api.Application.Closures.Dto;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ContaMec.Api.Application.Closures;

public class ClosureService(ContaMecDbContext dbContext) : IClosureService
{
    public async Task<List<ClosureResponse>> SearchAsync(ClosureSearchRequest request)
    {
        request ??= new ClosureSearchRequest();
        var closureTableName = await ResolveTableNameAsync("Closures", "Cierres");
        if (closureTableName is null)
            return [];

        var incomesTableName = await ResolveTableNameAsync("Incomes", "Ingresos");
        if (incomesTableName is null)
            return [];

        var expensesTableName = await ResolveTableNameAsync("Expenses", "Egresos");
        if (expensesTableName is null)
            return [];

        var closureBalancesTableName = await ResolveTableNameAsync("ClosureBalances", "BalanceCierres");
        if (closureBalancesTableName is null)
            return [];

        var closures = new List<ClosureResponse>();
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT c.[Id],
       c.[OpenDate],
       p.[PreviousBalance],
       COALESCE(i.[Incomes], 0) AS [Incomes],
       COALESCE(e.[Expenses], 0) AS [Expenses],
       COALESCE(b.[InAccount], 0) AS [InAccount],
       c.[CloseDate],
       c.[IsClosed],
       CASE
           WHEN c.[IsClosed] = 1 THEN c.[Result]
           ELSE COALESCE(p.[PreviousBalance], 0) + COALESCE(i.[Incomes], 0) - COALESCE(e.[Expenses], 0)
       END AS [Result]
FROM [{closureTableName}] c
OUTER APPLY (
    SELECT TOP 1 prev.[Result] AS [PreviousBalance]
    FROM [{closureTableName}] prev
    WHERE prev.[Id] < c.[Id]
    ORDER BY prev.[Id] DESC
) p
OUTER APPLY (
    SELECT SUM(x.[Amount]) AS [Incomes]
    FROM [{incomesTableName}] x
    WHERE x.[ClosureId] = c.[Id]
) i
OUTER APPLY (
    SELECT SUM(x.[Amount]) AS [Expenses]
    FROM [{expensesTableName}] x
    WHERE x.[ClosureId] = c.[Id]
) e
OUTER APPLY (
    SELECT SUM(x.[Amount]) AS [InAccount]
    FROM [{closureBalancesTableName}] x
    WHERE x.[ClosureId] = c.[Id]
) b
WHERE (@id IS NULL OR c.[Id] = @id)
  AND (@isClosed IS NULL OR c.[IsClosed] = @isClosed)
  AND (@openDateFrom IS NULL OR c.[OpenDate] >= @openDateFrom)
  AND (@openDateToExclusive IS NULL OR c.[OpenDate] < @openDateToExclusive)
ORDER BY c.[Id] DESC";

            AddNullableParameter(command, "@id", DbType.Int32, request.Id);
            AddNullableParameter(command, "@isClosed", DbType.Boolean, request.IsClosed);
            AddNullableParameter(command, "@openDateFrom", DbType.DateTime, request.OpenDateFrom);
            AddNullableParameter(command, "@openDateToExclusive", DbType.DateTime, request.OpenDateTo?.Date.AddDays(1));

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                closures.Add(new ClosureResponse
                {
                    Id = reader.GetInt32(0),
                    OpenDate = reader.IsDBNull(1) ? null : reader.GetDateTime(1),
                    PreviousBalance = reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                    Incomes = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                    Expenses = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                    InAccount = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                    CloseDate = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                    IsClosed = reader.IsDBNull(7) ? null : reader.GetBoolean(7),
                    Result = reader.IsDBNull(8) ? null : reader.GetDecimal(8)
                });
            }
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }

        return closures;
    }

    public async Task<ClosureIncomeDetailResponse?> GetIncomeDetailAsync(int closureId)
    {
        if (closureId <= 0)
            return null;

        var closureTableName = await ResolveTableNameAsync("Closures", "Cierres");
        var incomesTableName = await ResolveTableNameAsync("Incomes", "Ingresos");
        var accountsTableName = await ResolveTableNameAsync("Accounts", "Cuentas");
        if (closureTableName is null || incomesTableName is null || accountsTableName is null)
            return null;

        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            await using (var existsCommand = connection.CreateCommand())
            {
                existsCommand.CommandText = $"SELECT TOP 1 1 FROM [{closureTableName}] WHERE [Id] = @closureId";
                AddNullableParameter(existsCommand, "@closureId", DbType.Int32, closureId);
                var exists = await existsCommand.ExecuteScalarAsync();
                if (exists is null)
                    return null;
            }

            var rows = new List<ClosureIncomeDetailRowResponse>();
            decimal totalIncomes = 0;

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = $@"
SELECT i.[AccountId],
       MAX(a.[Name]) AS [AccountName],
       SUM(i.[Amount]) AS [Amount]
FROM [{incomesTableName}] i
LEFT JOIN [{accountsTableName}] a ON a.[Id] = i.[AccountId]
WHERE i.[ClosureId] = @closureId
GROUP BY i.[AccountId]
ORDER BY [Amount] DESC, i.[AccountId] ASC";
                AddNullableParameter(command, "@closureId", DbType.Int32, closureId);

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var amount = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);
                    rows.Add(new ClosureIncomeDetailRowResponse
                    {
                        AccountId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        AccountName = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Amount = amount,
                        Percentage = 0
                    });
                    totalIncomes += amount;
                }
            }

            if (totalIncomes > 0)
            {
                foreach (var row in rows)
                {
                    row.Percentage = Math.Round((row.Amount / totalIncomes) * 100m, 2, MidpointRounding.AwayFromZero);
                }
            }

            return new ClosureIncomeDetailResponse
            {
                ClosureId = closureId,
                TotalIncomes = totalIncomes,
                Items = rows
            };
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    public async Task<ClosureExpenseDetailResponse?> GetExpenseDetailAsync(int closureId)
    {
        if (closureId <= 0)
            return null;

        var closureTableName = await ResolveTableNameAsync("Closures", "Cierres");
        var expensesTableName = await ResolveTableNameAsync("Expenses", "Egresos");
        var accountsTableName = await ResolveTableNameAsync("Accounts", "Cuentas");
        if (closureTableName is null || expensesTableName is null || accountsTableName is null)
            return null;

        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            await using (var existsCommand = connection.CreateCommand())
            {
                existsCommand.CommandText = $"SELECT TOP 1 1 FROM [{closureTableName}] WHERE [Id] = @closureId";
                AddNullableParameter(existsCommand, "@closureId", DbType.Int32, closureId);
                var exists = await existsCommand.ExecuteScalarAsync();
                if (exists is null)
                    return null;
            }

            var rows = new List<ClosureExpenseDetailRowResponse>();
            decimal totalExpenses = 0;

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = $@"
SELECT e.[AccountId],
       MAX(a.[Name]) AS [AccountName],
       SUM(e.[Amount]) AS [Amount]
FROM [{expensesTableName}] e
LEFT JOIN [{accountsTableName}] a ON a.[Id] = e.[AccountId]
WHERE e.[ClosureId] = @closureId
GROUP BY e.[AccountId]
ORDER BY [Amount] DESC, e.[AccountId] ASC";
                AddNullableParameter(command, "@closureId", DbType.Int32, closureId);

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var amount = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);
                    rows.Add(new ClosureExpenseDetailRowResponse
                    {
                        AccountId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        AccountName = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Amount = amount,
                        Percentage = 0
                    });
                    totalExpenses += amount;
                }
            }

            if (totalExpenses > 0)
            {
                foreach (var row in rows)
                {
                    row.Percentage = Math.Round((row.Amount / totalExpenses) * 100m, 2, MidpointRounding.AwayFromZero);
                }
            }

            return new ClosureExpenseDetailResponse
            {
                ClosureId = closureId,
                TotalExpenses = totalExpenses,
                Items = rows
            };
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    public async Task<ClosureCloseResponse> CloseAsync(int closureId)
    {
        if (closureId <= 0)
            throw new ArgumentException("El Id del cierre es inválido.");

        var closureTableName = await ResolveTableNameAsync("Closures", "Cierres");
        var incomesTableName = await ResolveTableNameAsync("Incomes", "Ingresos");
        var expensesTableName = await ResolveTableNameAsync("Expenses", "Egresos");
        var closureBalancesTableName = await ResolveTableNameAsync("ClosureBalances", "BalanceCierres");

        if (closureTableName is null || incomesTableName is null || expensesTableName is null || closureBalancesTableName is null)
            throw new ArgumentException("No se pudieron resolver las tablas necesarias para cerrar el registro.");

        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            bool isClosed;
            await using (var existsCommand = connection.CreateCommand())
            {
                existsCommand.Transaction = transaction;
                existsCommand.CommandText = $"SELECT TOP 1 COALESCE([IsClosed], 0) FROM [{closureTableName}] WHERE [Id] = @closureId";
                AddNullableParameter(existsCommand, "@closureId", DbType.Int32, closureId);
                var rawIsClosed = await existsCommand.ExecuteScalarAsync();
                if (rawIsClosed is null)
                    throw new ArgumentException("El cierre indicado no existe.");

                isClosed = Convert.ToBoolean(rawIsClosed);
            }

            if (isClosed)
                throw new ArgumentException("El cierre seleccionado ya se encuentra cerrado.");

            decimal previousBalance;
            decimal incomes;
            decimal expenses;
            decimal inAccount;

            await using (var calcCommand = connection.CreateCommand())
            {
                calcCommand.Transaction = transaction;
                calcCommand.CommandText = $@"
SELECT
    COALESCE((
        SELECT TOP 1 prev.[Result]
        FROM [{closureTableName}] prev
        WHERE prev.[Id] < @closureId
        ORDER BY prev.[Id] DESC
    ), 0) AS [PreviousBalance],
    COALESCE((SELECT SUM(i.[Amount]) FROM [{incomesTableName}] i WHERE i.[ClosureId] = @closureId), 0) AS [Incomes],
    COALESCE((SELECT SUM(e.[Amount]) FROM [{expensesTableName}] e WHERE e.[ClosureId] = @closureId), 0) AS [Expenses],
    COALESCE((SELECT SUM(b.[Amount]) FROM [{closureBalancesTableName}] b WHERE b.[ClosureId] = @closureId), 0) AS [InAccount]";
                AddNullableParameter(calcCommand, "@closureId", DbType.Int32, closureId);

                await using var reader = await calcCommand.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    throw new ArgumentException("No se pudieron calcular los valores del cierre.");

                previousBalance = reader.IsDBNull(0) ? 0m : reader.GetDecimal(0);
                incomes = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
                expenses = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2);
                inAccount = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3);
            }

            var result = previousBalance + incomes - expenses;
            var difference = inAccount - result;
            if (Math.Abs(difference) > 0.009m)
                throw new ArgumentException("Solo se puede cerrar cuando la diferencia es igual a cero.");

            var now = DateTime.Now;
            await using (var updateCommand = connection.CreateCommand())
            {
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = $@"
UPDATE [{closureTableName}]
SET [CloseDate] = @closeDate,
    [Result] = @result,
    [IsClosed] = 1
WHERE [Id] = @closureId";
                AddNullableParameter(updateCommand, "@closeDate", DbType.DateTime, now);
                AddNullableParameter(updateCommand, "@result", DbType.Decimal, result);
                AddNullableParameter(updateCommand, "@closureId", DbType.Int32, closureId);

                var rows = await updateCommand.ExecuteNonQueryAsync();
                if (rows == 0)
                    throw new ArgumentException("No se pudo actualizar el cierre.");
            }

            int newClosureId;
            await using (var insertCommand = connection.CreateCommand())
            {
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = $@"
INSERT INTO [{closureTableName}] ([OpenDate], [CloseDate], [IsClosed], [Result])
VALUES (@openDate, NULL, 0, NULL);
SELECT CAST(SCOPE_IDENTITY() AS int);";
                AddNullableParameter(insertCommand, "@openDate", DbType.DateTime, now);

                var scalar = await insertCommand.ExecuteScalarAsync();
                if (scalar is null)
                    throw new ArgumentException("No se pudo crear el nuevo cierre.");

                newClosureId = Convert.ToInt32(scalar);
            }

            await transaction.CommitAsync();

            return new ClosureCloseResponse
            {
                ClosedClosureId = closureId,
                CloseDate = now,
                Result = result,
                NewClosureId = newClosureId,
                NewOpenDate = now
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private async Task<string?> ResolveTableNameAsync(string preferredName, string fallbackName)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            foreach (var candidate in new[] { preferredName, fallbackName })
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT TOP 1 1 FROM sys.tables WHERE [name] = @tableName";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@tableName";
                parameter.Value = candidate;
                command.Parameters.Add(parameter);

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
