namespace ContaMec.Api.Infrastructure.Data;

/// <summary>
/// Hora de reloj de Argentina, independiente de la zona horaria del servidor (Render corre en UTC).
/// Se guarda como el resto de las fechas: hora local sin zona (ver <see cref="DbDateTime"/>).
/// </summary>
public static class ArgentinaClock
{
    private static readonly TimeZoneInfo TimeZone = ResolveTimeZone();

    public static DateTime Now =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone), DateTimeKind.Unspecified);

    private static TimeZoneInfo ResolveTimeZone()
    {
        // IANA en Linux (y en Windows con ICU); el id de Windows como respaldo.
        foreach (var id in new[] { "America/Argentina/Buenos_Aires", "Argentina Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
        }

        // Argentina no tiene horario de verano desde 2009: UTC-3 fijo.
        return TimeZoneInfo.CreateCustomTimeZone("ART", TimeSpan.FromHours(-3), "Argentina", "Argentina");
    }
}
