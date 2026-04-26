using System;

namespace ContaMec.Api.Domain.Entities;

public class Income
{
    public int Id { get; set; }
    public int? ClosureId { get; set; }
    public DateTime? EmissionDate { get; set; }
    public int? AccountId { get; set; }
    public string? Detail { get; set; }
    public decimal? Amount { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? CreatedAt { get; set; }

    public Closure? Closure { get; set; }
    public Account? Account { get; set; }
    public User? CreatedByUser { get; set; }
}
