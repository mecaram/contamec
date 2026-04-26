namespace ContaMec.Api.Application.Auth;

public class AuthSettings
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Solo desarrollo: si el hash en BD no es BCrypt, permite comprobar contraseña en texto plano
    /// para poder iniciar sesión hasta migrar PasswordHash con BCrypt.HashPassword.
    /// En producción debe ser false.
    /// </summary>
    public bool AllowPlaintextPasswordForDevelopment { get; set; }
}
