using ContaMec.Api.Application.ClosureBalances;
using ContaMec.Api.Application.ClosureBalances.Dto;
using Microsoft.AspNetCore.Mvc;

namespace ContaMec.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Valores en cuenta")]
public class ClosureBalancesController(IClosureBalanceService closureBalanceService) : ControllerBase
{
    [HttpGet("by-closure/{closureId:int}")]
    [ProducesResponseType(typeof(List<ClosureBalanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByClosureId(int closureId)
    {
        try
        {
            var result = await closureBalanceService.GetByClosureIdAsync(closureId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("by-closure/{closureId:int}/bulk")]
    [ProducesResponseType(typeof(List<ClosureBalanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveBulk(int closureId, [FromBody] List<ClosureBalanceSaveRequest>? items)
    {
        if (items is null)
            return BadRequest(new { message = "Cuerpo JSON requerido." });

        try
        {
            var result = await closureBalanceService.SaveBulkAsync(closureId, items);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
