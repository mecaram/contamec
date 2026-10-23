using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContaMec.Api.Application.Orders;

/// <summary>Datos de la empresa para el encabezado de los reportes.</summary>
public record ReportCompany(string Name, string? Document, string? Address)
{
    public static async Task<ReportCompany> LoadAsync(ContaMecDbContext dbContext, int applicationId)
    {
        var app = await dbContext.Applications
            .AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => new { a.Name, a.DocumentType, a.DocumentNumber, a.Address, a.Locality })
            .FirstOrDefaultAsync();
        if (app is null)
            return new ReportCompany(string.Empty, null, null);

        var document = app.DocumentType.HasValue && !string.IsNullOrWhiteSpace(app.DocumentNumber)
            ? $"{app.DocumentType}: {app.DocumentNumber}"
            : null;
        var address = string.Join(", ", new[] { app.Address, app.Locality }.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));
        return new ReportCompany(app.Name, document, string.IsNullOrEmpty(address) ? null : address);
    }
}
