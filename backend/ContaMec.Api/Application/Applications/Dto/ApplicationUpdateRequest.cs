namespace ContaMec.Api.Application.Applications.Dto;

public class ApplicationUpdateRequest
{
    public string? Name { get; set; }
    public bool Active { get; set; } = true;
}
