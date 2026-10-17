using ContaMec.Api.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContaMec.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Autenticación (inicio)")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido: { \"username\", \"password\" }.");

        try
        {
            var result = await authService.LoginAsync(request, cancellationToken);
            if (result is null)
                return Unauthorized();

            return Ok(result);
        }
        catch (LoginRejectedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}
