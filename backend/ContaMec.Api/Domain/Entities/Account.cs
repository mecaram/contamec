using System.Collections.Generic;

namespace ContaMec.Api.Domain.Entities;

public class Account
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public AccountType? Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ApplicationId { get; set; }
    public bool? SubAccountRequires { get; set; }
    public bool? Active { get; set; }
    public BehaviorType? BehaviorType { get; set; }

    public Application? Application { get; set; }

    public ICollection<Income> Incomes { get; set; } = new List<Income>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<SubAccount> SubAccounts { get; set; } = new List<SubAccount>();
}
