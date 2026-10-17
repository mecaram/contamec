using System.Collections.Generic;

namespace ContaMec.Api.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int UserRoleId { get; set; }
    public int? ApplicationId { get; set; }

    public UserRole UserRole { get; set; } = null!;
    public Application? Application { get; set; }

    public ICollection<Income> IncomesCreated { get; set; } = new List<Income>();
    public ICollection<Expense> ExpensesCreated { get; set; } = new List<Expense>();
}
