namespace ContaMec.Api.Domain.Entities;

/// <summary>Historial de planes: un registro por cada plan activado desde el formulario Planes.</summary>
public class Plan
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ApplicationId { get; set; }
    public ApplicationType? Type { get; set; }

    /// <summary>Desde cuándo rige este período: el día siguiente al vencimiento anterior si seguía vigente, o la fecha de activación.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Último día del plan, inclusive: el PlanExpirationDate que quedó en la empresa.</summary>
    public DateTime? ExpirationDate { get; set; }
}
