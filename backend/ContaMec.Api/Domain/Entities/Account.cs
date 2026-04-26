using System.Collections.Generic;

namespace ContaMec.Api.Domain.Entities;

public class Account
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }

    public ICollection<Income> Incomes { get; set; } = new List<Income>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
