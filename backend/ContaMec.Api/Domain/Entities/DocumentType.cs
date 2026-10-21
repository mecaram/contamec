namespace ContaMec.Api.Domain.Entities;

/// <summary>
/// Enum "DocumentTypes" de PostgreSQL (Supabase). Los nombres deben coincidir exactamente con las etiquetas del enum.
/// </summary>
public enum DocumentType
{
    CUIL,
    CUIT,
    DNI
}
