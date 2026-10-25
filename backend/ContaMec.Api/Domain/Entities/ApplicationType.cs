using NpgsqlTypes;

namespace ContaMec.Api.Domain.Entities;

/// <summary>
/// Enum "ApplicationTypes" de PostgreSQL (Supabase). Los nombres deben coincidir con las etiquetas del enum;
/// la que no es un identificador válido de C# se indica con PgName. Una etiqueta de la base que falte acá
/// hace fallar la lectura de cualquier Application que la use (por ejemplo, el login de sus usuarios).
/// </summary>
public enum ApplicationType
{
    Demo,
    [PgName("Pro+")]
    ProPlus
}
