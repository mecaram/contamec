using ContaMec.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        try
        {
            // Una consulta real (no solo abrir la conexión) obliga a Entity Framework a armar el modelo,
            // la parte más lenta del primer pedido tras un arranque.
            await dbContext.Users.AsNoTracking().AnyAsync(cancellationToken);
            return Ok(new { status = "ok" });
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "database_unavailable" });
        }
    }
}
