namespace ContaMec.Api.Application.EmailTemplates.Dto;

public class EmailTemplateResponse
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public DateTime CreatedAt { get; set; }
}
