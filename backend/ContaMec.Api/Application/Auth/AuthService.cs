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
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Name == request.Username, cancellationToken);

        if (user is null || !user.IsActive)
            return null;

        if (!VerifyPassword(request.Password, user.PasswordHash))
            return null;

        var name = user.Name ?? string.Empty;
        var token = CreateJwt(user.Id, name);
        return new LoginResponse { Token = token, Username = name };
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

    private string CreateJwt(int userId, string name)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpiresInMinutes <= 0 ? 60 : _jwt.ExpiresInMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
