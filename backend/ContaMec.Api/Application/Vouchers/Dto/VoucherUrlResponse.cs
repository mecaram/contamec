namespace ContaMec.Api.Application.Vouchers.Dto;

public class VoucherUrlResponse
{
    /// <summary>URL firmada, válida por unos minutos.</summary>
    public string Url { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
    public bool IsImage { get; set; }
}
