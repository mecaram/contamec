using ContaMec.Api.Application.Applications;
using ContaMec.Api.Application.Applications.Dto;
using ContaMec.Api.Application.Auth;
using ContaMec.Api.Application.CompanyRegistration.Dto;
using ContaMec.Api.Application.Notifications;
using ContaMec.Api.Application.Users;
using ContaMec.Api.Domain.Entities;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net;
// Alias: "Application" choca con el namespace ContaMec.Api.Application.
using ApplicationEntity = ContaMec.Api.Domain.Entities.Application;

namespace ContaMec.Api.Application.CompanyRegistration;

/// <summary>
/// Registro público: empresa, cierre inicial y usuario administrador, todos inactivos hasta que un SuperAdministrador los habilite.
/// Reutiliza las altas de <see cref="IApplicationService"/> e <see cref="IUserService"/> (mismas validaciones, cierre y hash).
/// </summary>
public class CompanyRegistrationService(
    ContaMecDbContext dbContext,
    IApplicationService applicationService,
    IUserService userService,
    IEmailNotifier emailNotifier,
    IEmailTemplateService emailTemplates) : ICompanyRegistrationService
{
    private readonly ContaMecDbContext _dbContext = dbContext;

    public async Task RegisterAsync(CompanyRegistrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // En el registro público el documento es obligatorio; el formato lo valida el alta de la empresa.
        if (string.IsNullOrWhiteSpace(request.DocumentType))
            throw new ArgumentException("Debe seleccionar el tipo de documento.");
        if (string.IsNullOrWhiteSpace(request.DocumentNumber))
            throw new ArgumentException("Debe ingresar el número de documento.");

        var application = await applicationService.AddAsync(new ApplicationCreateRequest
        {
            Name = request.Name,
            Active = false,
            WhatsApp = request.WhatsApp,
            Email = request.Email,
            Address = request.Address,
            Locality = request.Locality,
            DocumentType = request.DocumentType,
            DocumentNumber = request.DocumentNumber
        });

        var administratorRoleId = await _dbContext.UserRoles
            .Where(r => r.Name == UserRoleNames.Administrator)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException($"No existe el rol '{UserRoleNames.Administrator}'.");

        if (!string.IsNullOrEmpty(request.Password) && request.Password != request.ConfirmPassword)
            throw new ArgumentException("La contraseña y su repetición no coinciden.");

        var user = await userService.AddAsync(request.Username, request.Password, false, administratorRoleId, isSuperAdministrator: false);
        user.Application = application;

        // Un único SaveChanges corre en una transacción: si falla cualquier insert no queda empresa, cierre ni usuario sueltos.
        try
        {
            await userService.SaveChangesCheckingDuplicateNameAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "Applications_DocumentNumber_key" })
        {
            // Dos solicitudes simultáneas con el mismo documento: la validación previa no alcanza a verlo.
            throw new ArgumentException("Ya existe una empresa registrada con ese número de documento.");
        }

        // Después de confirmar: un aviso fallido no debe deshacer ni demorar indefinidamente el registro.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var email = await emailTemplates.RenderAsync(TemplateType.PedidoDemo, BuildNewCompanyValues(application, user.Name), timeout.Token)
            ?? new RenderedEmail($"ContaMec - Nueva empresa registrada: {application.Name}", BuildNewCompanyEmail(application, user.Name));
        await emailNotifier.NotifyAsync(email.Subject, email.Body, timeout.Token);
    }

    /// <summary>Valores de los marcadores de la plantilla PedidoDemo.</summary>
    private static Dictionary<string, string?> BuildNewCompanyValues(ApplicationEntity application, string username)
    {
        static string OrNotInformed(string? value) => string.IsNullOrWhiteSpace(value) ? "No informado" : value;

        return new Dictionary<string, string?>
        {
            ["Empresa"] = application.Name,
            ["Direccion"] = OrNotInformed(application.Address),
            ["Localidad"] = OrNotInformed(application.Locality),
            ["WhatsApp"] = OrNotInformed(string.IsNullOrWhiteSpace(application.WhatsApp) ? null : "+" + application.WhatsApp),
            ["Email"] = OrNotInformed(application.Email),
            ["Usuario"] = username,
            ["FechaRegistro"] = $"{application.CreatedAt:dd/MM/yyyy HH:mm}"
        };
    }

    /// <summary>Texto por defecto si no hay plantilla PedidoDemo en EmailTemplates.</summary>

    private static string BuildNewCompanyEmail(ApplicationEntity application, string username)
    {
        static string Row(string label, string? value) =>
            $"""
             <tr>
               <td style="padding:8px 12px;border-bottom:1px solid #e2e8f0;color:#64748b;width:40%">{label}</td>
               <td style="padding:8px 12px;border-bottom:1px solid #e2e8f0;color:#0f172a;font-weight:600">{(string.IsNullOrWhiteSpace(value) ? "No informado" : WebUtility.HtmlEncode(value))}</td>
             </tr>
             """;

        var whatsApp = string.IsNullOrWhiteSpace(application.WhatsApp) ? null : "+" + application.WhatsApp;

        return $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:0 auto;color:#0f172a">
              <div style="background:#0c4a6e;color:#fff;padding:20px 24px;border-radius:8px 8px 0 0">
                <div style="font-size:20px;font-weight:700">ContaMec</div>
                <div style="font-size:15px;opacity:.85">Nueva empresa registrada</div>
              </div>
              <div style="border:1px solid #e2e8f0;border-top:0;padding:24px;border-radius:0 0 8px 8px">
                <p style="margin:0 0 16px">Se registró una nueva empresa desde el formulario público de ContaMec. Estos son los datos informados:</p>
                <table style="width:100%;border-collapse:collapse;font-size:14px">
                  {Row("Empresa", application.Name)}
                  {Row("Dirección", application.Address)}
                  {Row("Localidad", application.Locality)}
                  {Row("WhatsApp", whatsApp)}
                  {Row("Email", application.Email)}
                  {Row("Usuario administrador", username)}
                  {Row("Fecha de registro", $"{application.CreatedAt:dd/MM/yyyy HH:mm} hs")}
                </table>
                <p style="margin:20px 0 0;padding:12px 16px;background:#fffbeb;border:1px solid #fde68a;border-radius:6px;color:#92400e">
                  La empresa y su usuario administrador quedaron <strong>inactivos</strong>, pendientes de habilitación.
                  Para habilitarlos, ingrese a ContaMec con un usuario SuperAdministrador desde <strong>Aplicaciones</strong> y <strong>Usuarios</strong>.
                </p>
                <p style="margin:20px 0 0;font-size:12px;color:#94a3b8">Este es un aviso automático de ContaMec; no responda a este correo.</p>
              </div>
            </div>
            """;
    }
}
