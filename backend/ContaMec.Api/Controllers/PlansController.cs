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
}
