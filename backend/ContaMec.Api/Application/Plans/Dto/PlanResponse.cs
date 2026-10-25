namespace ContaMec.Api.Application.Plans.Dto;

/// <summary>Plan de una empresa (Application): tipo, inicio y vencimiento.</summary>
public class PlanResponse
{
    public int ApplicationId { get; set; }
    public string? ApplicationName { get; set; }
    public string? Address { get; set; }
    public string? Locality { get; set; }
    public string? WhatsApp { get; set; }
    public bool Active { get; set; }

    /// <summary>"Demo" o "Pro+"; null si la empresa no tiene plan.</summary>
    public string? Plan { get; set; }

    /// <summary>Fecha en que se asignó el plan actual (ApplicationTypeDate).</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Último día del plan, inclusive (ApplicationExpirationDate); null si no vence.</summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>Días hasta el vencimiento contra la fecha de Argentina: 0 si vence hoy, negativo si venció; null si no vence.</summary>
    public int? DaysLeft { get; set; }
}
