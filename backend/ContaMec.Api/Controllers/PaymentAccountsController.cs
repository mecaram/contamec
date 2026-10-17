using ContaMec.Api.Application.Auth;
using ContaMec.Api.Application.PaymentAccounts;
using ContaMec.Api.Application.PaymentAccounts.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContaMec.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Tags("Cuentas de pago")]
public class PaymentAccountsController(IPaymentAccountService paymentAccountService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<PaymentAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "id")] int? id,
        [FromQuery(Name = "name")] string? name)
    {
        var request = new PaymentAccountSearchRequest
        {
            Id = id,
            Name = name
        };

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var result = await paymentAccountService.SearchAsync(request, applicationId);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PaymentAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(int id)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var result = await paymentAccountService.GetByIdAsync(id, applicationId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentAccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] PaymentAccountCreateRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var created = await paymentAccountService.CreateAsync(request, applicationId);
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
    public async Task<IActionResult> Update(int id, [FromBody] PaymentAccountUpdateRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var updated = await paymentAccountService.UpdateAsync(id, request, applicationId);
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
            var deleted = await paymentAccountService.DeleteAsync(id, applicationId);
            return deleted ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
