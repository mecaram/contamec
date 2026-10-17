namespace ContaMec.Api.Application.Applications.Dto;

public class ApplicationCreateRequest
{
    public string? Name { get; set; }
    public bool Active { get; set; } = true;
}
