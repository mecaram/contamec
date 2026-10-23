using ContaMec.Api.Application.Auth;
using ContaMec.Api.Application.EmailTemplates;
using ContaMec.Api.Application.EmailTemplates.Dto;
using ContaMec.Api.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContaMec.Api.Controllers;

[ApiController]
[Authorize(Roles = UserRoleNames.SuperAdministrator)]
[Route("api/[controller]")]
[Tags("Plantillas de correo")]
public class EmailTemplatesController(IEmailTemplateAdminService emailTemplateService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<EmailTemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "id")] int? id,
        [FromQuery(Name = "name")] string? name)
    {
        var request = new EmailTemplateSearchRequest
        {
            Id = id,
            Name = name
        };

        try
        {
            var result = await emailTemplateService.SearchAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("export/{format}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportList(
        string format,
        [FromQuery(Name = "id")] int? id,
        [FromQuery(Name = "name")] string? name)
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
        var request = new EmailTemplateSearchRequest { Id = id, Name = name };

        try
        {
            var file = await emailTemplateService.ExportListAsync(request, currentApplicationId, exportFormat.Value);
            return File(file, contentType, EmailTemplateListReport.FileName(extension));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmailTemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await emailTemplateService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EmailTemplateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] EmailTemplateSaveRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        try
        {
            var created = await emailTemplateService.CreateAsync(request);
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
    public async Task<IActionResult> Update(int id, [FromBody] EmailTemplateSaveRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        try
        {
            var updated = await emailTemplateService.UpdateAsync(id, request);
            return updated ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await emailTemplateService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
