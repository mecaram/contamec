using ContaMec.Api.Application.Accounts;
using ContaMec.Api.Application.Accounts.Dto;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ContaMec.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Cuentas")]
public class AccountsController(ContaMecDbContext dbContext, IAccountService accountService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<AccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "id")] int? id,
        [FromQuery(Name = "name")] string? name,
        [FromQuery(Name = "type")] string? type)
    {
        var request = new AccountSearchRequest
        {
            Id = id,
            Name = name,
            Type = type
        };

        var result = await accountService.SearchAsync(request);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await accountService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] AccountCreateRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        try
        {
            var created = await accountService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] AccountUpdateRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        try
        {
            var updated = await accountService.UpdateAsync(id, request);
            return updated ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await accountService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("income-options")]
    [ProducesResponseType(typeof(List<IncomeAccountOptionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIncomeOptions()
    {
        var tableName = await ResolveAccountsTableNameAsync();
        if (tableName is null)
            return Ok(new List<IncomeAccountOptionResponse>());

        var accounts = new List<IncomeAccountOptionResponse>();
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT [Id], [Name]
FROM [{tableName}]
WHERE [Type] IS NOT NULL AND [Type] LIKE @typeFilter
ORDER BY [Name] ASC, [Id] ASC";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@typeFilter";
            parameter.Value = "%Ingreso%";
            command.Parameters.Add(parameter);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                accounts.Add(new IncomeAccountOptionResponse
                {
                    Id = reader.GetInt32(0),
                    Name = reader.IsDBNull(1) ? null : reader.GetString(1)
                });
            }
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }

        return Ok(accounts);
    }

    [HttpGet("expense-options")]
    [ProducesResponseType(typeof(List<IncomeAccountOptionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpenseOptions()
    {
        var tableName = await ResolveAccountsTableNameAsync();
        if (tableName is null)
            return Ok(new List<IncomeAccountOptionResponse>());

        var accounts = new List<IncomeAccountOptionResponse>();
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT [Id], [Name]
FROM [{tableName}]
WHERE [Type] IS NOT NULL
  AND (
    [Type] COLLATE Latin1_General_CI_AI LIKE @egresoFilter
    OR [Type] COLLATE Latin1_General_CI_AI LIKE @gastoFilter
  )
ORDER BY [Name] ASC, [Id] ASC";

            var egresoParameter = command.CreateParameter();
            egresoParameter.ParameterName = "@egresoFilter";
            egresoParameter.Value = "%egreso%";
            command.Parameters.Add(egresoParameter);

            var gastoParameter = command.CreateParameter();
            gastoParameter.ParameterName = "@gastoFilter";
            gastoParameter.Value = "%gasto%";
            command.Parameters.Add(gastoParameter);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                accounts.Add(new IncomeAccountOptionResponse
                {
                    Id = reader.GetInt32(0),
                    Name = reader.IsDBNull(1) ? null : reader.GetString(1)
                });
            }
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }

        return Ok(accounts);
    }

    private async Task<string?> ResolveAccountsTableNameAsync()
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            foreach (var candidate in new[] { "Accounts", "Cuentas" })
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

    public class IncomeAccountOptionResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
