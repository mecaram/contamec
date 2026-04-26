using System;
using System.Collections.Generic;

namespace ContaMec.Api.Domain.Entities;

public class Closure
{
    public int Id { get; set; }
    public DateTime? OpenDate { get; set; }
    public DateTime? CloseDate { get; set; }
    public bool? IsClosed { get; set; }
    public decimal? Result { get; set; }

    public ICollection<Income> Incomes { get; set; } = new List<Income>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<ClosureBalance> Balances { get; set; } = new List<ClosureBalance>();
}
