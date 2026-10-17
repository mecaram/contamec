using System.Security.Claims;

namespace ContaMec.Api.Application.Auth;

/// <summary>
/// Acceso centralizado a los datos del usuario autenticado que viajan en el JWT.
/// El ApplicationId se toma siempre del token firmado, nunca de un valor enviado por el cliente.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public const string ApplicationIdClaim = "application_id";

    public static bool TryGetApplicationId(this ClaimsPrincipal principal, out int applicationId) =>
        int.TryParse(principal.FindFirstValue(ApplicationIdClaim), out applicationId);

    public static bool TryGetUserId(this ClaimsPrincipal principal, out int userId) =>
        int.TryParse(
            principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"),
            out userId);
}
