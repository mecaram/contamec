namespace ContaMec.Api.Application.Incomes.Dto;

public class IncomeSearchRequest
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? ClosureId { get; set; }
    public int? AccountId { get; set; }
    public decimal? AmountFrom { get; set; }
    public decimal? AmountTo { get; set; }
    public string? Detail { get; set; }
}
