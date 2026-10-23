using ContaMec.Api.Application.Auth;
using ContaMec.Api.Application.Expenses;
using ContaMec.Api.Application.Expenses.Dto;
using ContaMec.Api.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ContaMec.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Tags("Egresos")]
public class ExpensesController(IExpenseService expenseService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<ExpenseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "dateFrom")] DateTime? dateFrom,
        [FromQuery(Name = "dateTo")] DateTime? dateTo,
        [FromQuery(Name = "closureId")] int? closureId,
        [FromQuery(Name = "accountId")] int? accountId,
        [FromQuery(Name = "paymentAccountId")] int? paymentAccountId,
        [FromQuery(Name = "subAccountId")] int? subAccountId,
        [FromQuery(Name = "amountFrom")] decimal? amountFrom,
        [FromQuery(Name = "amountTo")] decimal? amountTo,
        [FromQuery(Name = "detail")] string? detail)
    {
        var request = new ExpenseSearchRequest
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            ClosureId = closureId,
            AccountId = accountId,
            PaymentAccountId = paymentAccountId,
            SubAccountId = subAccountId,
            AmountFrom = amountFrom,
            AmountTo = amountTo,
            Detail = detail
        };

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var result = await expenseService.SearchAsync(request, applicationId);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(int id)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var result = await expenseService.GetByIdAsync(id, applicationId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("last-payment-account")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLastPaymentAccount()
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new { message = "Usuario no autenticado." });

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var paymentAccountId = await expenseService.GetLastPaymentAccountIdAsync(currentUserId, applicationId);
        return Ok(new { paymentAccountId });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] ExpenseCreateRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new { message = "Usuario no autenticado." });

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var created = await expenseService.CreateAsync(request, currentUserId, applicationId);
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] ExpenseUpdateRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var updated = await expenseService.UpdateAsync(id, request, applicationId);
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var deleted = await expenseService.DeleteAsync(id, applicationId);
            return deleted ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrderPdf(int id)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var pdf = await expenseService.GetOrderPdfAsync(id, applicationId);
        return pdf is null ? NotFound() : File(pdf, "application/pdf", OrderPdf.FileName(OrderKind.Expense, id));
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
