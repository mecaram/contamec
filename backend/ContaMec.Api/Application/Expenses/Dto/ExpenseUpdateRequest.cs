namespace ContaMec.Api.Application.Expenses.Dto;

public class ExpenseUpdateRequest
{
    public DateTime? EmissionDate { get; set; }
    public int? AccountId { get; set; }
    public string? Detail { get; set; }
    public decimal? Amount { get; set; }
}
