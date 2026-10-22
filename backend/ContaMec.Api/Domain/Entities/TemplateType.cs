namespace ContaMec.Api.Domain.Entities;

/// <summary>
/// Enum "TemplateTypes" de PostgreSQL (Supabase). Los nombres deben coincidir exactamente con las etiquetas del enum.
/// </summary>
public enum TemplateType
{
    PedidoDemo,
    PedidoDemoAprobado,
    PedidoProduccion,
    PedidoProduccionAprobado,
    PedidoRenovacion,
    PedidoRenovacionAprobado
}
