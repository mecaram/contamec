namespace ContaMec.Api.Application.Applications.Dto;

public class ApplicationResponse
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool Active { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Locality { get; set; }
    public string? Guid { get; set; }
    public DateTime CreatedAt { get; set; }
}
