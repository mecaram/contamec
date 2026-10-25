namespace ContaMec.Api.Application.Closures.Dto;

public class ClosureResponse
{
    public int Id { get; set; }
    public int? ClosureNumber { get; set; }
    public DateTime? OpenDate { get; set; }
    public decimal? PreviousBalance { get; set; }
    public decimal Incomes { get; set; }
    public decimal Expenses { get; set; }
    /// <summary>Flujo neto del cierre: ingresos menos egresos, sin el saldo inicial.</summary>
    public decimal NetFlow => Incomes - Expenses;
    public decimal InAccount { get; set; }
    public DateTime? CloseDate { get; set; }
    public bool? IsClosed { get; set; }
    public decimal? Result { get; set; }
    /// <summary>Mes (yyyy-MM) en que el cierre tuvo más ingresos y egresos; null si no tiene movimientos.</summary>
    public string? PredominantMonth { get; set; }
}
