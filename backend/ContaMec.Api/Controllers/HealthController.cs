using ContaMec.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContaMec.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
[Tags("Estado")]
public class HealthController(ContaMecDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Despierta la API y abre la conexión a la base antes de que se use: el Login lo llama al cargarse,
    /// para que el arranque en frío no caiga sobre el inicio de sesión.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var databaseAvailable = await dbContext.Database.CanConnectAsync(cancellationToken);
        return databaseAvailable
            ? Ok(new { status = "ok" })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "database_unavailable" });
    }
}
