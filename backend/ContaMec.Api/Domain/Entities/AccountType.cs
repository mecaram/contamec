namespace ContaMec.Api.Domain.Entities;

/// <summary>
/// Enum "AccountTypes" de PostgreSQL (Supabase). Los nombres deben coincidir exactamente con las etiquetas del enum.
/// </summary>
public enum AccountType
{
    Ingresos,
    Egresos,
    Ambos
}
