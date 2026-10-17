namespace ContaMec.Api.Application.Applications.Dto;

public class ApplicationSearchRequest
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public bool? Active { get; set; }
}
