namespace ContaMec.Api.Application.Vouchers;

/// <summary>
/// Supabase Storage para los comprobantes adjuntos. El bucket es privado: solo la API lo usa, con la clave service_role,
/// y el navegador recibe URLs firmadas que vencen a los pocos minutos.
/// </summary>
public class SupabaseStorageSettings
{
    public const string SectionName = "SupabaseStorage";

    /// <summary>URL del proyecto, por ejemplo https://xxxx.supabase.co.</summary>
    public string? Url { get; set; }

    /// <summary>Secreto: va en User Secrets o en variables de entorno, nunca en appsettings.json ni en el frontend.</summary>
    public string? ServiceRoleKey { get; set; }

    public string VoucherBucket { get; set; } = "vouchers";

    /// <summary>Segundos que dura la URL firmada para ver o descargar un comprobante.</summary>
    public int SignedUrlSeconds { get; set; } = 300;
}
