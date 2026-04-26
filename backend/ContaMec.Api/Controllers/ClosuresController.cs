using ContaMec.Api.Application.Closures;
using ContaMec.Api.Application.Closures.Dto;
using Microsoft.AspNetCore.Mvc;

namespace ContaMec.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Cierres")]
public class ClosuresController(IClosureService closureService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<ClosureResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "id")] int? id,
        [FromQuery(Name = "isClosed")] bool? isClosed,
        [FromQuery(Name = "openDateFrom")] DateTime? openDateFrom,
        [FromQuery(Name = "openDateTo")] DateTime? openDateTo)
    {
        if (openDateFrom.HasValue && openDateTo.HasValue && openDateFrom.Value.Date > openDateTo.Value.Date)
        {
            return BadRequest(new { message = "La fecha desde no puede ser mayor a la fecha hasta." });
        }

        var request = new ClosureSearchRequest
        {
            Id = id,
            IsClosed = isClosed,
            OpenDateFrom = openDateFrom,
            OpenDateTo = openDateTo
        };

        var result = await closureService.SearchAsync(request);
        return Ok(result);
    }

    [HttpGet("{closureId:int}/income-detail")]
    [ProducesResponseType(typeof(ClosureIncomeDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIncomeDetail(int closureId)
    {
        var result = await closureService.GetIncomeDetailAsync(closureId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{closureId:int}/expense-detail")]
    [ProducesResponseType(typeof(ClosureExpenseDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExpenseDetail(int closureId)
    {
        var result = await closureService.GetExpenseDetailAsync(closureId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{closureId:int}/close")]
    [ProducesResponseType(typeof(ClosureCloseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Close(int closureId)
    {
        try
        {
            var result = await closureService.CloseAsync(closureId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
