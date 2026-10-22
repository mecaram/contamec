namespace ContaMec.Api.Application.EmailTemplates.Dto;

/// <summary>Alta y edición: Name es una etiqueta del enum TemplateTypes; Subject y Body admiten marcadores {{Clave}}.</summary>
public class EmailTemplateSaveRequest
{
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
}
