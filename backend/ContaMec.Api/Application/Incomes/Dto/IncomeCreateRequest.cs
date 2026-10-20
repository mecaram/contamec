namespace ContaMec.Api.Application.Incomes.Dto;

public class IncomeCreateRequest
{
    public DateTime? EmissionDate { get; set; }
    public int? AccountId { get; set; }
    public string? Detail { get; set; }
    public decimal? Amount { get; set; }
    public int? PaymentAccountId { get; set; }
    public int? SubAccountId { get; set; }
    public string? OccurrenceType { get; set; }
}
