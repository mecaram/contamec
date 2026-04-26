using System.Collections.Generic;

namespace ContaMec.Api.Domain.Entities;

public class PaymentAccount
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<ClosureBalance> ClosureBalances { get; set; } = new List<ClosureBalance>();
}
