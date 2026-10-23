using ContaMec.Api.Application.Auth;
using ContaMec.Api.Application.Closures;
using ContaMec.Api.Application.Closures.Dto;
using ContaMec.Api.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContaMec.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Tags("Cierres")]
public class ClosuresController(IClosureService closureService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<ClosureResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "id")] int? id,
        [FromQuery(Name = "closureNumber")] int? closureNumber,
        [FromQuery(Name = "isClosed")] bool? isClosed,
        [FromQuery(Name = "openDateFrom")] DateTime? openDateFrom,
        [FromQuery(Name = "openDateTo")] DateTime? openDateTo)
    {
        if (openDateFrom.HasValue && openDateTo.HasValue && openDateFrom.Value.Date > openDateTo.Value.Date)
        {
            return BadRequest(new { message = "La fecha desde no puede ser mayor a la fecha hasta." });
        }

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var request = new ClosureSearchRequest
        {
            Id = id,
            ClosureNumber = closureNumber,
            IsClosed = isClosed,
            OpenDateFrom = openDateFrom,
            OpenDateTo = openDateTo
        };

        var result = await closureService.SearchAsync(request, applicationId);
        return Ok(result);
    }

    [HttpGet("export/{format}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportList(
        string format,
        [FromQuery(Name = "id")] int? id,
        [FromQuery(Name = "closureNumber")] int? closureNumber,
        [FromQuery(Name = "isClosed")] bool? isClosed,
        [FromQuery(Name = "openDateFrom")] DateTime? openDateFrom,
        [FromQuery(Name = "openDateTo")] DateTime? openDateTo)
    {
        if (openDateFrom.HasValue && openDateTo.HasValue && openDateFrom.Value.Date > openDateTo.Value.Date)
            return BadRequest(new { message = "La fecha desde no puede ser mayor a la fecha hasta." });

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

        var request = new ClosureSearchRequest
        {
            Id = id,
            ClosureNumber = closureNumber,
            IsClosed = isClosed,
            OpenDateFrom = openDateFrom,
            OpenDateTo = openDateTo
        };
        var file = await closureService.ExportListAsync(request, applicationId, exportFormat.Value);
        return File(file, contentType, ClosureListReport.FileName(extension));
    }

    [HttpGet("{closureId:int}/income-detail")]
    [ProducesResponseType(typeof(ClosureIncomeDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIncomeDetail(int closureId)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var result = await closureService.GetIncomeDetailAsync(closureId, applicationId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{closureId:int}/expense-detail")]
    [ProducesResponseType(typeof(ClosureExpenseDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExpenseDetail(int closureId)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var result = await closureService.GetExpenseDetailAsync(closureId, applicationId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{closureId:int}/close")]
    [ProducesResponseType(typeof(ClosureCloseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Close(int closureId)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var result = await closureService.CloseAsync(closureId, applicationId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
