using NpgsqlTypes;

namespace ContaMec.Api.Domain.Entities;

/// <summary>
/// Enum "ApplicationTypes" de PostgreSQL (Supabase). Los nombres deben coincidir con las etiquetas del enum;
/// la que lleva tilde se indica con PgName.
/// </summary>
public enum ApplicationType
{
    Demo,
    [PgName("Producción")]
    Produccion
}
