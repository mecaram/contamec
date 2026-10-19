using System.Collections.Generic;

namespace ContaMec.Api.Domain.Entities;

public class SubAccount
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? AccountId { get; set; }
    public bool? Active { get; set; }
    public BehaviorType? BehaviorType { get; set; }

    public Account? Account { get; set; }

    public ICollection<Income> Incomes { get; set; } = new List<Income>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
