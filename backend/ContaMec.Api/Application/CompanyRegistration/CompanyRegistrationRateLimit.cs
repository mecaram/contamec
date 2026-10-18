namespace ContaMec.Api.Application.CompanyRegistration;

/// <summary>Límite de solicitudes por IP del endpoint público de registro de empresas.</summary>
public static class CompanyRegistrationRateLimit
{
    public const string PolicyName = "company-registration";
    public const int PermitLimit = 5;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
}
