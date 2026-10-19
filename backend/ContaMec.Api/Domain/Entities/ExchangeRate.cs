using System.Collections.Generic;

namespace ContaMec.Api.Domain.Entities;

public class ExchangeRate
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RateDate { get; set; }
    public RateType? RateType { get; set; }
    public decimal? BuyRate { get; set; }
    public decimal? SellRate { get; set; }
    public int? CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }

    public ICollection<Income> Incomes { get; set; } = new List<Income>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
