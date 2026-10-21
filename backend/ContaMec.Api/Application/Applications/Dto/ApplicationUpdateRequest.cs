namespace ContaMec.Api.Application.Applications.Dto;

public class ApplicationUpdateRequest
{
    public string? Name { get; set; }
    public bool Active { get; set; } = true;
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Locality { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
}
