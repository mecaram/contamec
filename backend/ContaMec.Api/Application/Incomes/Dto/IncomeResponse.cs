namespace ContaMec.Api.Application.Incomes.Dto;

public class IncomeResponse
{
    public int Id { get; set; }
    public int? ClosureId { get; set; }
    public DateTime? EmissionDate { get; set; }
    public int? AccountId { get; set; }
    public string? AccountName { get; set; }
    public string? Detail { get; set; }
    public decimal? Amount { get; set; }
    public int? PaymentAccountId { get; set; }
    public string? PaymentAccountName { get; set; }
    public int? SubAccountId { get; set; }
    public string? SubAccountName { get; set; }
    public string? OccurrenceType { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? CreatedAt { get; set; }
    /// <summary>Tiene comprobante adjunto; la URL se pide aparte (GET api/incomes/{id}/voucher) porque vence.</summary>
    public bool HasVoucher { get; set; }
}
