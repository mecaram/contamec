namespace ContaMec.Api.Domain.Entities;

/// <summary>Plantilla de correo: Subject y Body llevan marcadores {{Nombre}} que se reemplazan al enviar.</summary>
public class EmailTemplate
{
    public int Id { get; set; }
    public TemplateType? Name { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public DateTime CreatedAt { get; set; }
}
