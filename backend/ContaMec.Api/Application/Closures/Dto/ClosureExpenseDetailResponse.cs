namespace ContaMec.Api.Application.Closures.Dto;

public class ClosureExpenseDetailResponse
{
    public int ClosureId { get; set; }
    public decimal TotalExpenses { get; set; }
    public List<ClosureExpenseDetailRowResponse> Items { get; set; } = [];
}

public class ClosureExpenseDetailRowResponse
{
    public int AccountId { get; set; }
    public string? AccountName { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}
