namespace ContaMec.Api.Application.Closures.Dto;

public class ClosureResponse
{
    public int Id { get; set; }
    public DateTime? OpenDate { get; set; }
    public decimal? PreviousBalance { get; set; }
    public decimal Incomes { get; set; }
    public decimal Expenses { get; set; }
    public decimal InAccount { get; set; }
    public DateTime? CloseDate { get; set; }
    public bool? IsClosed { get; set; }
    public decimal? Result { get; set; }
}
