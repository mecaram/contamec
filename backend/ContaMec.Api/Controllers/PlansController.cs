using ContaMec.Api.Application.Auth;
using ContaMec.Api.Application.Plans;
using ContaMec.Api.Application.Plans.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContaMec.Api.Controllers;

[ApiController]
[Authorize(Roles = UserRoleNames.SuperAdministrator)]
[Route("api/[controller]")]
[Tags("Planes")]
public class PlansController(IPlanService planService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<PlanResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "applicationId")] int? applicationId,
        [FromQuery(Name = "applicationName")] string? applicationName,
        [FromQuery(Name = "locality")] string? locality,
        [FromQuery(Name = "active")] bool? active,
        [FromQuery(Name = "plan")] string? plan,
        [FromQuery(Name = "expiration")] string? expiration)
    {
        var request = new PlanSearchRequest
        {
            ApplicationId = applicationId,
            ApplicationName = applicationName,
            Locality = locality,
            Active = active,
            Plan = plan,
            Expiration = expiration
        };

        return Ok(await planService.SearchAsync(request));
    }

    /// <summary>Opciones del filtro Localidad.</summary>
    [HttpGet("localities")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocalities() => Ok(await planService.GetLocalitiesAsync());

    /// <summary>Historial de planes activados para la empresa, del más reciente al más antiguo.</summary>
    [HttpGet("{applicationId:int}/history")]
    [ProducesResponseType(typeof(List<PlanHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(int applicationId) => Ok(await planService.GetHistoryAsync(applicationId));

    /// <summary>Elimina el último plan de la empresa y le vuelve a asignar el anterior, o la deja sin plan si no hay.</summary>
    [HttpDelete("{applicationId:int}/last")]
    [ProducesResponseType(typeof(PlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLast(int applicationId)
    {
        try
        {
            var result = await planService.DeleteLastAsync(applicationId);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Activa la empresa y sus usuarios y le asigna el plan Demo por 7 días más.</summary>
    [HttpPost("{applicationId:int}/demo")]
    [ProducesResponseType(typeof(PlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateDemo(int applicationId)
    {
        var result = await planService.ActivateDemoAsync(applicationId);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Activa la empresa y sus usuarios y le asigna el plan Pro+ por 30 días más.</summary>
    [HttpPost("{applicationId:int}/pro-plus")]
    [ProducesResponseType(typeof(PlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateProPlus(int applicationId)
    {
        var result = await planService.ActivateProPlusAsync(applicationId);
        return result is null ? NotFound() : Ok(result);
    }
}
