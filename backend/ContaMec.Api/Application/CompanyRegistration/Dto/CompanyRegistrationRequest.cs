namespace ContaMec.Api.Application.CompanyRegistration.Dto;

/// <summary>
/// Alta pública de una empresa y su usuario administrador.
/// No expone Active, rol ni ApplicationId: los asigna siempre el backend.
/// </summary>
public class CompanyRegistrationRequest
{
    public string? Name { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Locality { get; set; }

    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
}
