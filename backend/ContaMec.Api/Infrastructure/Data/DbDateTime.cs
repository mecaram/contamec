using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ContaMec.Api.Infrastructure.Data;

/// <summary>
/// Las columnas de fecha en Supabase son timestamptz, pero la app maneja "hora de reloj" sin zona
/// (como el datetime de SQL Server). Se guarda el mismo valor marcado como UTC, sin convertir,
/// y se lee de vuelta como Unspecified, así no depende de la zona horaria del servidor.
/// </summary>
public static class DbDateTime
{
    public static DateTime ToDb(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static DateTime? ToDb(DateTime? value) => value.HasValue ? ToDb(value.Value) : null;

    public static DateTime FromDb(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
}

public class WallClockDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => DbDateTime.ToDb(value),
    value => DbDateTime.FromDb(value));
