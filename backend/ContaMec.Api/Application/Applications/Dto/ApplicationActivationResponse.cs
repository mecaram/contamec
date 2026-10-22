namespace ContaMec.Api.Application.Applications.Dto;

public class ApplicationActivationResponse
{
    /// <summary>Email de la empresa al que se envió el aviso de activación; null si no tiene.</summary>
    public string? Email { get; set; }
    public bool EmailSent { get; set; }
}
