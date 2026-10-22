namespace ContaMec.Api.Application.CompanyRegistration.Dto;

/// <summary>
/// Alta pública de una empresa y su usuario administrador.
/// No expone Active, rol ni ApplicationId: los asigna siempre el backend.
/// </summary>
public class CompanyRegistrationRequest
{
    public string? Name { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    /// <summary>Repetición del email para detectar errores de tipeo. Si viene, debe coincidir con <see cref="Email"/>.</summary>
    public string? ConfirmEmail { get; set; }
    public string? Address { get; set; }
    public string? Locality { get; set; }

    public string? Username { get; set; }
    /// <summary>Repetición del usuario para detectar errores de tipeo. Si viene, debe coincidir con <see cref="Username"/>.</summary>
    public string? ConfirmUsername { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
}
