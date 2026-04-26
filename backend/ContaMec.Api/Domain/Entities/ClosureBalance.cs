namespace ContaMec.Api.Domain.Entities;

public class ClosureBalance
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public int PaymentAccountId { get; set; }
    public decimal? Amount { get; set; }

    public Closure Closure { get; set; } = null!;
    public PaymentAccount PaymentAccount { get; set; } = null!;
}
