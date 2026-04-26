namespace ContaMec.Api.Application.Closures.Dto;

public class ClosureIncomeDetailResponse
{
    public int ClosureId { get; set; }
    public decimal TotalIncomes { get; set; }
    public List<ClosureIncomeDetailRowResponse> Items { get; set; } = [];
}

public class ClosureIncomeDetailRowResponse
{
    public int AccountId { get; set; }
    public string? AccountName { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}
