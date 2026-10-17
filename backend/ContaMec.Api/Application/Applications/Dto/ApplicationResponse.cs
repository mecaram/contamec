namespace ContaMec.Api.Application.Applications.Dto;

public class ApplicationResponse
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
}
