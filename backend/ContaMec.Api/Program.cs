using System.Text;
using System.Threading.RateLimiting;
using ContaMec.Api.Application.Accounts;
using ContaMec.Api.Application.Applications;
using ContaMec.Api.Application.Auth;
using ContaMec.Api.Application.Closures;
using ContaMec.Api.Application.ClosureBalances;
using ContaMec.Api.Application.CompanyRegistration;
using ContaMec.Api.Application.Expenses;
using ContaMec.Api.Application.Incomes;
using ContaMec.Api.Application.Notifications;
using ContaMec.Api.Application.PaymentAccounts;
using ContaMec.Api.Application.Users;
using ContaMec.Api.Domain.Entities;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Npgsql.NameTranslation;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "AllowFrontend";

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection(AuthSettings.SectionName));
builder.Services.Configure<NotificationSettings>(builder.Configuration.GetSection(NotificationSettings.SectionName));
builder.Services.Configure<ResendSettings>(builder.Configuration.GetSection(ResendSettings.SectionName));

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                  ?? throw new InvalidOperationException($"La sección '{JwtSettings.SectionName}' no está configurada.");
if (string.IsNullOrWhiteSpace(jwtSettings.Key))
    throw new InvalidOperationException("Jwt:Key no puede estar vacío.");
var keyBytes = Encoding.UTF8.GetBytes(jwtSettings.Key);
if (keyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key debe tener al menos 32 bytes (UTF-8) para firmar con HS256. Amplíe la clave en appsettings o User Secrets.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            // Sin los 5 minutos de tolerancia por defecto: la sesión vence justo a Jwt:ExpiresInMinutes de inactividad.
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
                    context.Token = null;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// El registro público de empresas es anónimo: se limita por IP para frenar altas automatizadas.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(CompanyRegistrationRateLimit.PolicyName, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = CompanyRegistrationRateLimit.PermitLimit,
                Window = CompanyRegistrationRateLimit.Window,
                QueueLimit = 0
            }));
});

builder.Services.AddControllers();
var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
// Usuario y host nunca llevan espacios; al pegar la cadena (p. ej. en Render) suelen colarse por saltos de línea.
var csb = dataSourceBuilder.ConnectionStringBuilder;
csb.Username = RemoveWhitespace(csb.Username);
csb.Host = RemoveWhitespace(csb.Host);
// Sin esto, Npgsql cierra las conexiones con 5 minutos de inactividad y el siguiente pedido paga una conexión
// nueva a Supabase (~2 s). Se mantiene una abierta y el keepalive evita que el pooler la corte por inactividad.
if (csb.MinPoolSize == 0)
    csb.MinPoolSize = 1;
if (csb.KeepAlive == 0)
    csb.KeepAlive = 30;
dataSourceBuilder.MapEnum<AccountType>("AccountTypes", new NpgsqlNullNameTranslator());
var dataSource = dataSourceBuilder.Build();
builder.Services.AddDbContext<ContaMecDbContext>(options => options.UseNpgsql(dataSource));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClosureService, ClosureService>();
builder.Services.AddScoped<IClosureBalanceService, ClosureBalanceService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IIncomeService, IncomeService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IPaymentAccountService, PaymentAccountService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<ICompanyRegistrationService, CompanyRegistrationService>();
builder.Services.AddHttpClient<IEmailNotifier, ResendEmailNotifier>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ContaMec API",
        Version = "v1",
        Description =
            "Paso 1: expanda «Autenticación (inicio)» y ejecute POST /api/auth/login (username / password). " +
            "Paso 2: copie el token. " +
            "Paso 3: Authorize → Bearer {token}. " +
            "Luego puede usar otros endpoints con JWT."
    });
    c.OrderActionsBy(apiDesc =>
    {
        if (apiDesc.ActionDescriptor is ControllerActionDescriptor cad
            && string.Equals(cad.ControllerName, "Auth", StringComparison.OrdinalIgnoreCase))
            return "0";
        return "1_" + (apiDesc.RelativePath ?? apiDesc.ActionDescriptor.DisplayName);
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT en el encabezado Authorization: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

var enableSwagger = app.Environment.IsDevelopment()
    || app.Configuration.GetValue("Swagger:Enabled", true);

app.UseCors(CorsPolicyName);
app.UseHttpsRedirection();

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = string.Empty;
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ContaMec API v1");
        options.DocumentTitle = "ContaMec — Login: POST /api/auth/login";
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();

static string? RemoveWhitespace(string? value) =>
    value is null ? null : string.Concat(value.Where(c => !char.IsWhiteSpace(c)));
