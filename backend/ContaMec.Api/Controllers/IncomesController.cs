using ContaMec.Api.Application.Incomes;
using ContaMec.Api.Application.Incomes.Dto;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ContaMec.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Ingresos")]
public class IncomesController(IIncomeService incomeService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<IncomeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "dateFrom")] DateTime? dateFrom,
        [FromQuery(Name = "dateTo")] DateTime? dateTo,
        [FromQuery(Name = "closureId")] int? closureId,
        [FromQuery(Name = "accountId")] int? accountId,
        [FromQuery(Name = "amountFrom")] decimal? amountFrom,
        [FromQuery(Name = "amountTo")] decimal? amountTo,
        [FromQuery(Name = "detail")] string? detail)
    {
        var request = new IncomeSearchRequest
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            ClosureId = closureId,
            AccountId = accountId,
            AmountFrom = amountFrom,
            AmountTo = amountTo,
            Detail = detail
        };

        var result = await incomeService.SearchAsync(request);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(IncomeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await incomeService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(IncomeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] IncomeCreateRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new { message = "Usuario no autenticado." });

        try
        {
            var created = await incomeService.CreateAsync(request, currentUserId);
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
    public async Task<IActionResult> Update(int id, [FromBody] IncomeUpdateRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        try
        {
            var updated = await incomeService.UpdateAsync(id, request);
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
            var deleted = await incomeService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var claimValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("nameid");

        return int.TryParse(claimValue, out userId);
    }
}
