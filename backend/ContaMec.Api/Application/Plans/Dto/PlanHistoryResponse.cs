namespace ContaMec.Api.Application.Plans.Dto;

/// <summary>Un plan activado para la empresa (registro de la tabla Plans).</summary>
public class PlanHistoryResponse
{
    public int Id { get; set; }

    /// <summary>Cuándo se activó el plan.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>"Demo" o "Pro+".</summary>
    public string? Plan { get; set; }

    /// <summary>Desde cuándo rige el período.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Último día del período, inclusive.</summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>Días hasta el vencimiento contra la fecha de Argentina: 0 si vence hoy, negativo si venció; null si no vence.</summary>
    public int? DaysLeft { get; set; }
}
