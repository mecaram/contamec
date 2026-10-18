namespace ContaMec.Api.Application.Auth;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    /// <summary>Emite un token nuevo para una sesión vigente; null si el usuario o su aplicación ya no están activos.</summary>
    Task<RefreshTokenResponse?> RefreshAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
