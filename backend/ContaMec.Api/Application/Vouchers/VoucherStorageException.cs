namespace ContaMec.Api.Application.Vouchers;

/// <summary>Supabase Storage rechazó la operación o no está configurado. El mensaje se puede mostrar al usuario.</summary>
public class VoucherStorageException(string message) : Exception(message);
