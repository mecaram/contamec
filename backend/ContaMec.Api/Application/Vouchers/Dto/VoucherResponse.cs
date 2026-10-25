namespace ContaMec.Api.Application.Vouchers.Dto;

public class VoucherResponse
{
    /// <summary>Nombre del archivo en Storage (timestamp y extensión).</summary>
    public string FileName { get; set; } = string.Empty;

    public bool IsImage { get; set; }
}
