namespace ContaMec.Api.Application.Plans.Dto;

public class PlanSearchRequest
{
    public int? ApplicationId { get; set; }
    public string? ApplicationName { get; set; }
    public string? Locality { get; set; }
    public bool? Active { get; set; }

    /// <summary>"Demo", "ProPlus" o "none" (sin plan); vacío para todos.</summary>
    public string? Plan { get; set; }

    /// <summary>"current" (vigentes), "expired" (vencidos) o "none" (sin vencimiento); vacío para todos.</summary>
    public string? Expiration { get; set; }

    /// <summary>Vencimiento desde este día, inclusive (solo la fecha).</summary>
    public DateTime? ExpirationFrom { get; set; }

    /// <summary>Vencimiento hasta este día, inclusive (solo la fecha).</summary>
    public DateTime? ExpirationTo { get; set; }
}
