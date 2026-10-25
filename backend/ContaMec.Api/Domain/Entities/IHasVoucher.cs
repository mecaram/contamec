namespace ContaMec.Api.Domain.Entities;

/// <summary>Registro con comprobante adjunto: en Voucher se guarda la ruta del archivo dentro del bucket.</summary>
public interface IHasVoucher
{
    int Id { get; }
    string? Voucher { get; set; }
}
