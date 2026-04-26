namespace ContaMec.Api.Application.Incomes.Dto;

public class IncomeUpdateRequest
{
    public DateTime? EmissionDate { get; set; }
    public int? AccountId { get; set; }
    public string? Detail { get; set; }
    public decimal? Amount { get; set; }
}
