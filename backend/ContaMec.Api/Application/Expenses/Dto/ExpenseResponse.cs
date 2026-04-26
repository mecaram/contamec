namespace ContaMec.Api.Application.Expenses.Dto;

public class ExpenseResponse
{
    public int Id { get; set; }
    public int? ClosureId { get; set; }
    public DateTime? EmissionDate { get; set; }
    public int? AccountId { get; set; }
    public string? AccountName { get; set; }
    public string? Detail { get; set; }
    public decimal? Amount { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? CreatedAt { get; set; }
}
