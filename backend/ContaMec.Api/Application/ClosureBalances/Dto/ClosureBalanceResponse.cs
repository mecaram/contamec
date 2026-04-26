namespace ContaMec.Api.Application.ClosureBalances.Dto;

public class ClosureBalanceResponse
{
    public int? Id { get; set; }
    public int ClosureId { get; set; }
    public int PaymentAccountId { get; set; }
    public string PaymentAccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
