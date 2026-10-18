using System;

namespace ContaMec.Api.Domain.Entities;

public class Income
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public DateTime? EmissionDate { get; set; }
    public int AccountId { get; set; }
    public string? Detail { get; set; }
    public decimal? Amount { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? ApplicationId { get; set; }
    public int? PaymentAccountId { get; set; }

    public Closure Closure { get; set; } = null!;
    public Account Account { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public Application? Application { get; set; }
    public PaymentAccount? PaymentAccount { get; set; }
}
