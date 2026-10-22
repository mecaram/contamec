using System;
using System.Collections.Generic;

namespace ContaMec.Api.Domain.Entities;

public class Application
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool Active { get; set; } = true;
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Locality { get; set; }
    public string? Guid { get; set; }
    public ApplicationType? Type { get; set; }
    public DateTime? ApplicationTypeDate { get; set; }
    public DateTime? ApplicationExpirationDate { get; set; }
    public DocumentType? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }

    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<Closure> Closures { get; set; } = new List<Closure>();
    public ICollection<ClosureBalance> ClosureBalances { get; set; } = new List<ClosureBalance>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Income> Incomes { get; set; } = new List<Income>();
    public ICollection<PaymentAccount> PaymentAccounts { get; set; } = new List<PaymentAccount>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
