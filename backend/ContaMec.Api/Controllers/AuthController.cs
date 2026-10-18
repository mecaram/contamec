using ContaMec.Api.Application.Auth;
using ContaMec.Api.Application.CompanyRegistration;
using ContaMec.Api.Application.CompanyRegistration.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ContaMec.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Autenticación (inicio)")]
public class AuthController(IAuthService authService, ICompanyRegistrationService companyRegistrationService) : ControllerBase
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

    /// <summary>Registro público de una empresa y su administrador; ambos quedan inactivos y no se emite token.</summary>
    [HttpPost("register-company")]
    [AllowAnonymous]
    [EnableRateLimiting(CompanyRegistrationRateLimit.PolicyName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RegisterCompany([FromBody] CompanyRegistrationRequest? request)
    {
        if (request is null)
            return BadRequest(new { message = "Cuerpo JSON requerido." });

        try
        {
            await companyRegistrationService.RegisterAsync(request);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Renueva el token mientras el usuario usa el sistema: así la sesión vence por inactividad
    /// (Jwt:ExpiresInMinutes sin renovar) y no a un plazo fijo desde el inicio de sesión.
    /// </summary>
    [HttpPost("refresh")]
    [Authorize]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var result = await authService.RefreshAsync(userId, cancellationToken);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        if (!User.TryGetUserId(out var userId))
            return Forbid();

        try
        {
            var changed = await authService.ChangePasswordAsync(userId, request, cancellationToken);
            return changed ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
