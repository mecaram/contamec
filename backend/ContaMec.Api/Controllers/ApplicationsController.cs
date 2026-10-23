using ContaMec.Api.Application.Applications;
using ContaMec.Api.Application.Applications.Dto;
using ContaMec.Api.Application.Auth;
using ContaMec.Api.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContaMec.Api.Controllers;

[ApiController]
[Authorize(Roles = UserRoleNames.SuperAdministrator)]
[Route("api/[controller]")]
[Tags("Aplicaciones")]
public class ApplicationsController(IApplicationService applicationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<ApplicationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "id")] int? id,
        [FromQuery(Name = "name")] string? name,
        [FromQuery(Name = "active")] bool? active)
    {
        var request = new ApplicationSearchRequest
        {
            Id = id,
            Name = name,
            Active = active
        };

        var result = await applicationService.SearchAsync(request);
        return Ok(result);
    }

    [HttpGet("export/{format}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportList(
        string format,
        [FromQuery(Name = "id")] int? id,
        [FromQuery(Name = "name")] string? name,
        [FromQuery(Name = "active")] bool? active)
    {
        var (exportFormat, contentType, extension) = format.ToLowerInvariant() switch
        {
            "pdf" => (ListExportFormat.Pdf, "application/pdf", "pdf"),
            "excel" => (ListExportFormat.Excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            _ => (default(ListExportFormat?), "", "")
        };
        if (exportFormat is null)
            return BadRequest(new { message = "Formato no soportado: use pdf o excel." });

        int? currentApplicationId = User.TryGetApplicationId(out var applicationId) ? applicationId : null;
        var request = new ApplicationSearchRequest { Id = id, Name = name, Active = active };
        var file = await applicationService.ExportListAsync(request, currentApplicationId, exportFormat.Value);
        return File(file, contentType, ApplicationListReport.FileName(extension));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await applicationService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApplicationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ApplicationCreateRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        try
        {
            var created = await applicationService.CreateAsync(request);
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
    public async Task<IActionResult> Update(int id, [FromBody] ApplicationUpdateRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var updated = await applicationService.UpdateAsync(id, request, applicationId);
            return updated ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/activate")]
    [ProducesResponseType(typeof(ApplicationActivationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(int id)
    {
        try
        {
            var result = await applicationService.ActivateAsync(id);
            return result is null ? NotFound() : Ok(result);
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
            var deleted = await applicationService.DeleteAsync(id, applicationId);
            return deleted ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
