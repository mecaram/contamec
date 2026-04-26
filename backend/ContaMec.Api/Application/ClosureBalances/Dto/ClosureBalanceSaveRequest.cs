namespace ContaMec.Api.Application.ClosureBalances.Dto;

public class ClosureBalanceSaveRequest
{
    public int? Id { get; set; }
    public int ClosureId { get; set; }
    public int PaymentAccountId { get; set; }
    public decimal? Amount { get; set; }
}
