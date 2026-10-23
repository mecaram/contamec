using ContaMec.Api.Application.Auth;
using ContaMec.Api.Application.Orders;
using ContaMec.Api.Application.SubAccounts;
using ContaMec.Api.Application.SubAccounts.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContaMec.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Tags("Subcuentas")]
public class SubAccountsController(ISubAccountService subAccountService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<SubAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "id")] int? id,
        [FromQuery(Name = "name")] string? name,
        [FromQuery(Name = "accountId")] int? accountId)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var request = new SubAccountSearchRequest
        {
            Id = id,
            Name = name,
            AccountId = accountId
        };

        var result = await subAccountService.SearchAsync(request, applicationId);
        return Ok(result);
    }

    [HttpGet("export/{format}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportList(
        string format,
        [FromQuery(Name = "id")] int? id,
        [FromQuery(Name = "name")] string? name,
        [FromQuery(Name = "accountId")] int? accountId)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var (exportFormat, contentType, extension) = format.ToLowerInvariant() switch
        {
            "pdf" => (ListExportFormat.Pdf, "application/pdf", "pdf"),
            "excel" => (ListExportFormat.Excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            _ => (default(ListExportFormat?), "", "")
        };
        if (exportFormat is null)
            return BadRequest(new { message = "Formato no soportado: use pdf o excel." });

        var request = new SubAccountSearchRequest { Id = id, Name = name, AccountId = accountId };
        var file = await subAccountService.ExportListAsync(request, applicationId, exportFormat.Value);
        return File(file, contentType, SubAccountListReport.FileName(extension));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SubAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var result = await subAccountService.GetByIdAsync(id, applicationId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubAccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] SubAccountCreateRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var created = await subAccountService.CreateAsync(request, applicationId);
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] SubAccountUpdateRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var updated = await subAccountService.UpdateAsync(id, request, applicationId);
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var deleted = await subAccountService.DeleteAsync(id, applicationId);
            return deleted ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
