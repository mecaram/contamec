using System.Text;
using ContaMec.Api.Application.Accounts;
using ContaMec.Api.Application.Auth;
using ContaMec.Api.Application.Closures;
using ContaMec.Api.Application.ClosureBalances;
using ContaMec.Api.Application.Expenses;
using ContaMec.Api.Application.Incomes;
using ContaMec.Api.Application.PaymentAccounts;
using ContaMec.Api.Application.Users;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "AllowFrontend";

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection(AuthSettings.SectionName));

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
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
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

builder.Services.AddControllers();
builder.Services.AddDbContext<ContaMecDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClosureService, ClosureService>();
builder.Services.AddScoped<IClosureBalanceService, ClosureBalanceService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IIncomeService, IncomeService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IPaymentAccountService, PaymentAccountService>();
builder.Services.AddScoped<IUserService, UserService>();
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

app.MapControllers();

app.Run();
