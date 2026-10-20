using System.Collections.Generic;

namespace ContaMec.Api.Domain.Entities;

public class PaymentAccount
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? ApplicationId { get; set; }
    public bool? Active { get; set; }

    public Application? Application { get; set; }

    public ICollection<ClosureBalance> ClosureBalances { get; set; } = new List<ClosureBalance>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Income> Incomes { get; set; } = new List<Income>();
}
