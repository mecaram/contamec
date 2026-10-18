using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ContaMec.Api.Application.Auth;

public class AuthService(
    ContaMecDbContext dbContext,
    IOptions<JwtSettings> jwtOptions,
    IOptions<AuthSettings> authOptions) : IAuthService
{
    private readonly ContaMecDbContext _dbContext = dbContext;
    private readonly JwtSettings _jwt = jwtOptions.Value;
    private readonly AuthSettings _auth = authOptions.Value;

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return null;

        var user = await _dbContext.Users
            .Include(u => u.Application)
            .Include(u => u.UserRole)
            .FirstOrDefaultAsync(u => u.Name.ToLower() == request.Username.ToLower(), cancellationToken);

        if (user is null || !user.IsActive)
            return null;

        if (!VerifyPassword(request.Password, user.PasswordHash))
            return null;

        // La aplicación se valida después de la contraseña para no revelar datos a quien no conoce las credenciales.
        var application = user.Application;
        if (application is null)
            throw new LoginRejectedException(
                "No es posible iniciar sesión porque el usuario no tiene una aplicación válida asociada. Comuníquese con el administrador del sistema.");

        if (!application.Active)
            throw new LoginRejectedException(
                "No es posible iniciar sesión porque la aplicación asociada a su empresa no se encuentra activa. Comuníquese con el administrador del sistema.");

        var previousLoginAt = user.LastLoginAt;
        user.LastLoginAt = ArgentinaClock.Now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var name = user.Name ?? string.Empty;
        var roleName = user.UserRole?.Name ?? string.Empty;
        var token = CreateJwt(user.Id, name, application.Id, roleName);
        return new LoginResponse
        {
            Token = token,
            Username = name,
            ApplicationId = application.Id,
            ApplicationName = application.Name,
            Role = roleName,
            PreviousLoginAt = previousLoginAt
        };
    }

    public async Task<RefreshTokenResponse?> RefreshAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Se vuelve a leer el usuario: si lo desactivaron (a él o a su empresa) durante la sesión, no se renueva.
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Application)
            .Include(u => u.UserRole)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null || !user.IsActive || user.Application is null || !user.Application.Active)
            return null;

        var token = CreateJwt(user.Id, user.Name ?? string.Empty, user.Application.Id, user.UserRole?.Name ?? string.Empty);
        return new RefreshTokenResponse { Token = token };
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrEmpty(request.CurrentPassword))
            throw new ArgumentException("La contraseña actual es obligatoria.");
        if (string.IsNullOrEmpty(request.NewPassword))
            throw new ArgumentException("La contraseña nueva es obligatoria.");
        if (request.NewPassword != request.ConfirmPassword)
            throw new ArgumentException("La contraseña nueva y su repetición no coinciden.");

        PasswordPolicy.Validate(request.NewPassword);

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return false;

        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new ArgumentException("La contraseña actual es incorrecta.");
        if (request.NewPassword == request.CurrentPassword)
            throw new ArgumentException("La contraseña nueva debe ser distinta de la actual.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
            return false;

        if (IsBcryptHash(storedHash))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }
            catch (Exception)
            {
                return false;
            }
        }

        if (_auth.AllowPlaintextPasswordForDevelopment
            && string.Equals(password, storedHash, StringComparison.Ordinal))
            return true;

        return false;
    }

    private static bool IsBcryptHash(string hash) =>
        hash.Length >= 4
        && hash[0] == '$'
        && hash[1] == '2'
        && (hash[2] == 'a' || hash[2] == 'b' || hash[2] == 'y')
        && hash[3] == '$';

    private string CreateJwt(int userId, string name, int applicationId, string roleName)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, name),
            new Claim(ClaimsPrincipalExtensions.ApplicationIdClaim, applicationId.ToString()),
            new Claim(ClaimTypes.Role, roleName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpiresInMinutes <= 0 ? 30 : _jwt.ExpiresInMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
