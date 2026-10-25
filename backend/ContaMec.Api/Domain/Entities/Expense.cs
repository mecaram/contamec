using System;

namespace ContaMec.Api.Domain.Entities;

public class Expense : IHasVoucher
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public DateTime? EmissionDate { get; set; }
    public int AccountId { get; set; }
    public string? Detail { get; set; }
    public decimal? Amount { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? ApplicationId { get; set; }
    public int? PaymentAccountId { get; set; }
    public int? SubAccountId { get; set; }
    public OccurrenceType? OccurrenceType { get; set; }
    public int? ExchangeRateId { get; set; }
    /// <summary>Ruta del comprobante adjunto en el bucket de Storage (expenses/{Id}/{timestamp}.{ext}); null si no tiene.</summary>
    public string? Voucher { get; set; }

    public Closure Closure { get; set; } = null!;
    public Account Account { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public Application? Application { get; set; }
    public PaymentAccount? PaymentAccount { get; set; }
    public SubAccount? SubAccount { get; set; }
    public ExchangeRate? ExchangeRate { get; set; }
}
