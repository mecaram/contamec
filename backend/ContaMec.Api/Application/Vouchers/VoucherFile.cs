namespace ContaMec.Api.Application.Vouchers;

/// <summary>Archivo recibido para adjuntar como comprobante.</summary>
public record VoucherFile(string FileName, string ContentType, byte[] Content);
