using ContaMec.Api.Application.Applications.Dto;
using ContaMec.Api.Application.Orders;
using ContaMec.Api.Application.Notifications;
using ContaMec.Api.Application.Vouchers;
using ContaMec.Api.Domain.Entities;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Net;
using System.Text.RegularExpressions;
// Alias: "Application" choca con el namespace ContaMec.Api.Application.
using ApplicationEntity = ContaMec.Api.Domain.Entities.Application;

namespace ContaMec.Api.Application.Applications;

public class ApplicationService(
    ContaMecDbContext dbContext,
    IEmailNotifier emailNotifier,
    IEmailTemplateService emailTemplates,
    IOptions<NotificationSettings> notificationOptions,
    IVoucherService voucherService) : IApplicationService
{
    private readonly ContaMecDbContext _dbContext = dbContext;
    private readonly NotificationSettings _notifications = notificationOptions.Value;

    public async Task<List<ApplicationResponse>> SearchAsync(ApplicationSearchRequest request)
    {
        request ??= new ApplicationSearchRequest();

        var query = _dbContext.Applications.AsNoTracking().AsQueryable();

        if (request.Id.HasValue)
            query = query.Where(a => a.Id == request.Id.Value);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(a => a.Name.ToLower().Contains(name.ToLower()));
        }

        if (request.Active.HasValue)
            query = query.Where(a => a.Active == request.Active.Value);

        return await query
            .OrderBy(a => a.Name)
            .ThenBy(a => a.Id)
            .Select(MapToResponse())
            .ToListAsync();
    }

    // Mismos filtros que la búsqueda; el listado sale ordenado como en pantalla (Nombre ascendente).
    // El encabezado lleva los datos de la empresa del usuario que exporta.
    public async Task<byte[]> ExportListAsync(ApplicationSearchRequest request, int? currentApplicationId, ListExportFormat format)
    {
        request ??= new ApplicationSearchRequest();
        var company = currentApplicationId.HasValue
            ? await ReportCompany.LoadAsync(_dbContext, currentApplicationId.Value)
            : new ReportCompany(string.Empty, null, null);

        var data = new ApplicationListData
        {
            CompanyName = company.Name,
            CompanyDocument = company.Document,
            CompanyAddress = company.Address,
            Filters =
            [
                ("ID", request.Id?.ToString() ?? "-"),
                ("Nombre", string.IsNullOrWhiteSpace(request.Name) ? "-" : request.Name.Trim()),
                ("Estado", request.Active switch { true => "Activas", false => "Inactivas", null => "Todas" })
            ],
            Rows = await SearchAsync(request)
        };

        return format == ListExportFormat.Excel ? ApplicationListReport.GenerateExcel(data) : ApplicationListReport.GeneratePdf(data);
    }

    public async Task<ApplicationResponse?> GetByIdAsync(int id)
    {
        return await _dbContext.Applications
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(MapToResponse())
            .FirstOrDefaultAsync();
    }

    public async Task<ApplicationResponse> CreateAsync(ApplicationCreateRequest request)
    {
        var application = await AddAsync(request);
        await SaveChangesCheckingDuplicateDocumentAsync();

        var created = await GetByIdAsync(application.Id);
        return created ?? throw new InvalidOperationException("No se pudo leer la aplicación recién creada.");
    }

    public async Task<ApplicationEntity> AddAsync(ApplicationCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ValidateApplicationAsync(request.Name, null);
        ValidateEmail(request.Email);
        ValidateWhatsApp(request.WhatsApp);
        var (documentType, documentNumber) = ParseDocument(request.DocumentType, request.DocumentNumber);
        await EnsureDocumentNumberIsFreeAsync(documentNumber, null);

        var application = new ApplicationEntity
        {
            Name = request.Name!.Trim(),
            Active = request.Active,
            WhatsApp = Normalize(request.WhatsApp),
            Email = Normalize(request.Email),
            Address = Normalize(request.Address),
            Locality = Normalize(request.Locality),
            DocumentType = documentType,
            DocumentNumber = documentNumber,
            Guid = System.Guid.NewGuid().ToString(),
            CreatedAt = ArgentinaClock.Now
        };

        _dbContext.Applications.Add(application);
        // Toda empresa necesita un cierre abierto para registrar ingresos y egresos; se crea en la misma transacción.
        _dbContext.Closures.Add(new Closure
        {
            OpenDate = ArgentinaClock.Now,
            IsClosed = false,
            ClosureNumber = 1,
            Application = application
        });
        return application;
    }

    public async Task<bool> UpdateAsync(int id, ApplicationUpdateRequest request, int currentApplicationId)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ValidateApplicationAsync(request.Name, id);
        ValidateEmail(request.Email);
        ValidateWhatsApp(request.WhatsApp);
        var (documentType, documentNumber) = ParseDocument(request.DocumentType, request.DocumentNumber);
        await EnsureDocumentNumberIsFreeAsync(documentNumber, id);

        var application = await _dbContext.Applications.FirstOrDefaultAsync(a => a.Id == id);
        if (application is null)
            return false;

        // Desactivar la propia aplicación dejaría a todos sus usuarios (incluido el actual) sin acceso.
        if (id == currentApplicationId && !request.Active)
            throw new ArgumentException("No puede desactivar la aplicación con la que inició sesión.");

        application.Name = request.Name!.Trim();
        application.Active = request.Active;
        application.WhatsApp = Normalize(request.WhatsApp);
        application.Email = Normalize(request.Email);
        application.Address = Normalize(request.Address);
        application.Locality = Normalize(request.Locality);
        application.DocumentType = documentType;
        application.DocumentNumber = documentNumber;
        // Las aplicaciones creadas antes de agregar la columna no tienen Guid.
        application.Guid ??= System.Guid.NewGuid().ToString();

        await SaveChangesCheckingDuplicateDocumentAsync();
        return true;
    }

    /// <summary>
    /// Elimina una aplicación inactiva junto con todos sus datos. Las claves foráneas no borran en cascada,
    /// así que se borra de las tablas hijas hacia Applications, en una transacción: se borra todo o nada.
    /// </summary>
    /// <exception cref="ArgumentException">Es la aplicación de la sesión, está activa u otra aplicación depende de sus datos.</exception>
    public async Task<ApplicationDeleteResponse?> DeleteAsync(int id, int currentApplicationId)
    {
        var application = await _dbContext.Applications.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (application is null)
            return null;

        if (id == currentApplicationId)
            throw new ArgumentException("No puede eliminar la aplicación con la que inició sesión.");

        if (application.Active)
            throw new ArgumentException("No se puede eliminar la aplicación porque está activa. Desactívela antes de eliminarla.");

        var response = new ApplicationDeleteResponse();
        void Add(string table, string label, int count)
        {
            if (count > 0)
                response.Deleted.Add(new ApplicationDeletedTable { Table = table, Label = label, Count = count });
        }

        // Los comprobantes de ingresos y egresos se borran de Storage recién cuando la base confirmó el borrado.
        var voucherPaths = await _dbContext.Incomes
            .Where(x => x.ApplicationId == id && x.Voucher != null)
            .Select(x => x.Voucher)
            .ToListAsync();
        voucherPaths.AddRange(await _dbContext.Expenses
            .Where(x => x.ApplicationId == id && x.Voucher != null)
            .Select(x => x.Voucher)
            .ToListAsync());

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // Primero lo que referencia a cierres, cuentas, subcuentas y cuentas de pago.
            Add("ClosureBalances", "Saldos de cierre", await _dbContext.ClosureBalances.Where(x => x.ApplicationId == id).ExecuteDeleteAsync());
            Add("Incomes", "Ingresos", await _dbContext.Incomes.Where(x => x.ApplicationId == id).ExecuteDeleteAsync());
            Add("Expenses", "Egresos", await _dbContext.Expenses.Where(x => x.ApplicationId == id).ExecuteDeleteAsync());
            Add("SubAccounts", "Subcuentas", await _dbContext.SubAccounts.Where(x => x.Account != null && x.Account.ApplicationId == id).ExecuteDeleteAsync());
            Add("Accounts", "Cuentas", await _dbContext.Accounts.Where(x => x.ApplicationId == id).ExecuteDeleteAsync());
            Add("PaymentAccounts", "Cuentas de pago", await _dbContext.PaymentAccounts.Where(x => x.ApplicationId == id).ExecuteDeleteAsync());
            Add("Closures", "Cierres", await _dbContext.Closures.Where(x => x.ApplicationId == id).ExecuteDeleteAsync());

            // Las cotizaciones las usan todas las aplicaciones: se conservan y solo pierden el usuario que las cargó.
            response.UnlinkedExchangeRates = await _dbContext.ExchangeRates
                .Where(x => x.CreatedByUser != null && x.CreatedByUser.ApplicationId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.CreatedByUserId, (int?)null));

            Add("Users", "Usuarios", await _dbContext.Users.Where(x => x.ApplicationId == id).ExecuteDeleteAsync());
            Add("Plans", "Planes", await _dbContext.Plans.Where(x => x.ApplicationId == id).ExecuteDeleteAsync());
            Add("Applications", "Aplicaciones", await _dbContext.Applications.Where(x => x.Id == id).ExecuteDeleteAsync());

            await transaction.CommitAsync();
        }
        catch (Exception ex) when ((ex as PostgresException ?? ex.InnerException as PostgresException) is { SqlState: PostgresErrorCodes.ForeignKeyViolation } pg)
        {
            // Por ejemplo, un ingreso de otra aplicación cargado por un usuario de esta. La transacción se descarta.
            await transaction.RollbackAsync();
            throw new ArgumentException(
                $"No se puede eliminar la aplicación porque otra aplicación tiene registros que dependen de sus datos (tabla {pg.TableName}). No se borró nada.");
        }

        await voucherService.DeleteFilesAsync(voucherPaths);
        return response;
    }

    private const int DemoDays = 7;

    public async Task<ApplicationActivationResponse?> ActivateAsync(int id)
    {
        var application = await _dbContext.Applications
            .Include(a => a.Users)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (application is null)
            return null;

        if (application.Active)
            throw new ArgumentException("La aplicación ya está activa.");

        var now = ArgentinaClock.Now;
        application.Active = true;
        application.Plan = ApplicationType.Demo;
        application.PlanStartDate = now;
        application.PlanExpirationDate = now.AddDays(DemoDays);
        application.Guid ??= System.Guid.NewGuid().ToString();
        foreach (var user in application.Users)
            user.IsActive = true;

        // Un único SaveChanges: la aplicación y sus usuarios se activan juntos o no se activa nada.
        await _dbContext.SaveChangesAsync();

        var response = new ApplicationActivationResponse { Email = application.Email };
        if (application.Email is null)
            return response;

        // Después de confirmar: un aviso fallido no debe deshacer ni demorar indefinidamente la activación.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var email = await emailTemplates.RenderAsync(TemplateType.PedidoDemoAprobado, BuildActivationValues(application), timeout.Token)
            ?? new RenderedEmail($"ContaMec - Tu demo gratis está activa por {DemoDays} días", BuildActivationEmail(application));
        response.EmailSent = await emailNotifier.SendAsync(application.Email, email.Subject, email.Body, timeout.Token);
        return response;
    }

    /// <summary>Valores de los marcadores de la plantilla PedidoDemoAprobado.</summary>
    private Dictionary<string, string?> BuildActivationValues(ApplicationEntity application)
    {
        return new Dictionary<string, string?>
        {
            ["Empresa"] = application.Name,
            ["Usuario"] = JoinUsernames(application),
            ["DiasDemo"] = DemoDays.ToString(),
            ["FechaInicio"] = $"{application.PlanStartDate:dd/MM/yyyy HH:mm}",
            ["FechaVencimiento"] = $"{application.PlanExpirationDate:dd/MM/yyyy HH:mm}",
            ["LoginUrl"] = _notifications.LoginUrl,
            ["WhatsAppSoporte"] = FormatSupportWhatsApp(),
            ["WhatsAppSoporteNumero"] = _notifications.WhatsApp
        };
    }

    private static string JoinUsernames(ApplicationEntity application) =>
        string.Join(", ", application.Users.Select(u => u.Name).OrderBy(n => n));

    // Mismo formato que el listado de aplicaciones: +54 9 + código de área y número.
    private string? FormatSupportWhatsApp() =>
        _notifications.WhatsApp is { Length: 13 } digits && digits.StartsWith("549")
            ? $"+54 9 {digits[3..7]} {digits[7..]}"
            : _notifications.WhatsApp;

    /// <summary>Texto por defecto si no hay plantilla PedidoDemoAprobado en EmailTemplates.</summary>
    private string BuildActivationEmail(ApplicationEntity application)
    {
        static string Row(string label, string? value) =>
            $"""
             <tr>
               <td style="padding:8px 12px;border-bottom:1px solid #e2e8f0;color:#64748b;width:40%">{label}</td>
               <td style="padding:8px 12px;border-bottom:1px solid #e2e8f0;color:#0f172a;font-weight:600">{(string.IsNullOrWhiteSpace(value) ? "-" : WebUtility.HtmlEncode(value))}</td>
             </tr>
             """;

        var name = WebUtility.HtmlEncode(application.Name);
        var usernames = JoinUsernames(application);
        var loginUrl = WebUtility.HtmlEncode(_notifications.LoginUrl);
        var whatsApp = FormatSupportWhatsApp();
        var contact = string.IsNullOrWhiteSpace(whatsApp)
            ? ""
            : $"""
              <p style="margin:20px 0 0">Si tenés dudas o querés seguir usando ContaMec cuando termine la demo, escribinos por WhatsApp al <a href="https://wa.me/{_notifications.WhatsApp}" style="color:#0c4a6e;font-weight:600">{whatsApp}</a>.</p>
              """;

        return $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:0 auto;color:#0f172a">
              <div style="background:#0c4a6e;color:#fff;padding:20px 24px;border-radius:8px 8px 0 0">
                <div style="font-size:20px;font-weight:700">ContaMec</div>
                <div style="font-size:15px;opacity:.85">Tu cuenta ya está activa</div>
              </div>
              <div style="border:1px solid #e2e8f0;border-top:0;padding:24px;border-radius:0 0 8px 8px">
                <p style="margin:0 0 12px">Hola, <strong>{name}</strong>:</p>
                <p style="margin:0 0 16px">¡Bienvenido a ContaMec! Ya habilitamos tu cuenta y desde este momento podés usar el sistema durante <strong>{DemoDays} días</strong> en modo <strong>Demo</strong>, sin costo.</p>
                <table style="width:100%;border-collapse:collapse;font-size:14px">
                  {Row("Empresa", application.Name)}
                  {Row("Usuario", usernames)}
                  {Row("Inicio de la demo", $"{application.PlanStartDate:dd/MM/yyyy HH:mm} hs")}
                  {Row("Vence el", $"{application.PlanExpirationDate:dd/MM/yyyy HH:mm} hs")}
                </table>
                <p style="margin:24px 0;text-align:center">
                  <a href="{loginUrl}" style="display:inline-block;background:#0c4a6e;color:#fff;padding:12px 28px;border-radius:6px;text-decoration:none;font-weight:600">Ingresar a ContaMec</a>
                </p>
                <p style="margin:0">Ingresá con el usuario y la contraseña que elegiste al solicitar la demo.</p>
                {contact}
                <p style="margin:20px 0 0;font-size:12px;color:#94a3b8">Este es un correo automático de ContaMec; no respondas a este mensaje.</p>
              </div>
            </div>
            """;
    }

    private static Expression<Func<ApplicationEntity, ApplicationResponse>> MapToResponse()
    {
        return application => new ApplicationResponse
        {
            Id = application.Id,
            Name = application.Name,
            // Etiquetas del enum DocumentTypes de la base.
            DocumentType = application.DocumentType == DocumentType.CUIL ? nameof(DocumentType.CUIL)
                : application.DocumentType == DocumentType.CUIT ? nameof(DocumentType.CUIT)
                : application.DocumentType == DocumentType.DNI ? nameof(DocumentType.DNI)
                : null,
            DocumentNumber = application.DocumentNumber,
            Active = application.Active,
            WhatsApp = application.WhatsApp,
            Email = application.Email,
            Address = application.Address,
            Locality = application.Locality,
            Guid = application.Guid,
            CreatedAt = application.CreatedAt
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void ValidateEmail(string? email)
    {
        if (!string.IsNullOrWhiteSpace(email) && !new EmailAddressAttribute().IsValid(email.Trim()))
            throw new ArgumentException("El correo no tiene un formato válido.");
    }

    // Celular de Argentina en formato de WhatsApp: 549 + código de área y número (10 dígitos, sin 0 ni 15).
    private const string DuplicateDocumentMessage = "Ya existe una empresa registrada con ese número de documento.";

    // Solo por número: el CUIT y el CUIL de una misma persona comparten los 11 dígitos.
    private async Task EnsureDocumentNumberIsFreeAsync(string? documentNumber, int? currentId)
    {
        if (documentNumber is not null
            && await _dbContext.Applications.AnyAsync(a => a.DocumentNumber == documentNumber && (!currentId.HasValue || a.Id != currentId.Value)))
            throw new ArgumentException(DuplicateDocumentMessage);
    }

    /// <summary>El índice único Applications_DocumentNumber_key frena los duplicados simultáneos que la validación previa no ve.</summary>
    private async Task SaveChangesCheckingDuplicateDocumentAsync()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "Applications_DocumentNumber_key" })
        {
            throw new ArgumentException(DuplicateDocumentMessage);
        }
    }

    // Opcional, pero tipo y número van juntos. Etiquetas del enum DocumentTypes de la base: CUIL, CUIT, DNI.
    private static (DocumentType? Type, string? Number) ParseDocument(string? documentType, string? documentNumber)
    {
        var type = documentType?.Trim();
        var number = documentNumber?.Trim();
        if (string.IsNullOrEmpty(type) && string.IsNullOrEmpty(number))
            return (null, null);
        if (string.IsNullOrEmpty(type))
            throw new ArgumentException("Debe seleccionar el tipo de documento.");
        if (string.IsNullOrEmpty(number))
            throw new ArgumentException("Debe ingresar el número de documento.");
        if (!Enum.TryParse<DocumentType>(type, ignoreCase: true, out var value) || !Enum.IsDefined(value))
            throw new ArgumentException("El tipo de documento debe ser CUIL, CUIT o DNI.");
        if (!Regex.IsMatch(number, @"^\d+$"))
            throw new ArgumentException("El número de documento debe tener solo números.");
        if (value == DocumentType.DNI && number.Length is < 7 or > 8)
            throw new ArgumentException("El DNI debe tener 7 u 8 dígitos.");
        if (value != DocumentType.DNI && number.Length != 11)
            throw new ArgumentException($"El {value} debe tener 11 dígitos.");

        return (value, number);
    }

    private static void ValidateWhatsApp(string? whatsApp)
    {
        if (string.IsNullOrWhiteSpace(whatsApp))
            return;
        var value = whatsApp.Trim();
        if (!Regex.IsMatch(value, @"^549[1-9]\d{9}$") || value.StartsWith("54915"))
            throw new ArgumentException("WhatsApp debe ser un celular de Argentina: 549 + código de área y número, sin 0 ni 15.");
    }

    private async Task ValidateApplicationAsync(string? name, int? currentId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name es obligatorio.");
        if (name.Trim().Length > 100)
            throw new ArgumentException("Name no puede superar 100 caracteres.");

        var normalizedName = name.Trim();
        var exists = await _dbContext.Applications
            .AnyAsync(a => a.Name.ToLower() == normalizedName.ToLower() && (!currentId.HasValue || a.Id != currentId.Value));
        if (exists)
            throw new ArgumentException("Ya existe una aplicación con ese nombre.");
    }
}
