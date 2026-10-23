using System.Globalization;
using ContaMec.Api.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ContaMec.Api.Application.Orders;

public enum OrderKind
{
    Income,
    Expense
}

/// <summary>Datos de la Orden de Ingreso o de Egreso, leídos de la base en el momento de generarla.</summary>
public class OrderData
{
    public OrderKind Kind { get; set; }
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyDocument { get; set; }
    public string? CompanyAddress { get; set; }
    public DateTime? EmissionDate { get; set; }
    public int? ClosureNumber { get; set; }
    public string? AccountName { get; set; }
    public string? SubAccountName { get; set; }
    public string? OccurrenceType { get; set; }
    public string? Detail { get; set; }
    public decimal? Amount { get; set; }
    public string? PaymentAccountName { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public static class OrderPdf
{
    private static readonly CultureInfo Argentina = CultureInfo.GetCultureInfo("es-AR");
    private const string Accent = "#1E3A8A";
    private const string Muted = "#64748B";
    private const string Border = "#CBD5E1";

    /// <summary>El número de la orden es el Id del ingreso o egreso, con 8 dígitos: no cambia aunque se modifique el registro.</summary>
    public static string FormatNumber(int id) => id.ToString("D8", CultureInfo.InvariantCulture);

    public static string FileName(OrderKind kind, int id) =>
        $"{(kind == OrderKind.Income ? "Orden_Ingreso" : "Orden_Egreso")}_{FormatNumber(id)}.pdf";

    private static string Title(OrderKind kind) => kind == OrderKind.Income ? "ORDEN DE INGRESO" : "ORDEN DE EGRESO";

    public static byte[] Generate(OrderData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor("#0F172A"));

                page.Header().Element(c => ComposeHeader(c, data));
                page.Content().PaddingVertical(18).Element(c => ComposeContent(c, data));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8).FontColor(Muted));
                    text.Span($"Generado con ContaMEC el {ArgentinaNow():dd/MM/yyyy HH:mm}");
                });
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, OrderData data)
    {
        container.BorderBottom(1.5f).BorderColor(Accent).PaddingBottom(10).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(data.CompanyName).FontSize(15).Bold().FontColor(Accent);
                if (!string.IsNullOrWhiteSpace(data.CompanyDocument))
                    column.Item().Text(data.CompanyDocument).FontColor(Muted);
                if (!string.IsNullOrWhiteSpace(data.CompanyAddress))
                    column.Item().Text(data.CompanyAddress).FontColor(Muted);
            });

            row.ConstantItem(190).AlignRight().Column(column =>
            {
                column.Item().AlignRight().Text(Title(data.Kind)).FontSize(13).Bold();
                column.Item().AlignRight().Text($"N.º {FormatNumber(data.Id)}").FontSize(12).Bold().FontColor(Accent);
                column.Item().AlignRight().Text($"Fecha: {FormatDate(data.EmissionDate)}");
            });
        });
    }

    private static void ComposeContent(IContainer container, OrderData data)
    {
        container.Column(column =>
        {
            column.Spacing(14);

            column.Item().Border(1).BorderColor(Border).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(130);
                    columns.RelativeColumn();
                });

                AddRow(table, "Cierre", data.ClosureNumber.HasValue ? $"N.º {data.ClosureNumber}" : "-");
                AddRow(table, "Cuenta", data.AccountName);
                AddRow(table, "Subcuenta", data.SubAccountName);
                AddRow(table, "Ocurrencia", data.OccurrenceType);
                AddRow(table, "Detalle", data.Detail);
                AddRow(table, "Cuenta de pago", data.PaymentAccountName);
            });

            column.Item().Background("#EFF6FF").Border(1).BorderColor(Accent).Padding(10).Column(amount =>
            {
                amount.Item().Row(row =>
                {
                    row.RelativeItem().AlignMiddle().Text("Importe").FontSize(12).Bold();
                    row.RelativeItem().AlignRight().AlignMiddle()
                        .Text(data.Amount.HasValue ? data.Amount.Value.ToString("C2", Argentina) : "-")
                        .FontSize(16).Bold().FontColor(Accent);
                });
                if (data.Amount.HasValue)
                    amount.Item().PaddingTop(6).Text(AmountInWords.ToPesos(data.Amount.Value)).Italic();
            });

            column.Item().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(9).FontColor(Muted));
                text.Span($"Registrado por {Value(data.CreatedByUserName)}");
                if (data.CreatedAt.HasValue)
                    text.Span($" el {data.CreatedAt.Value:dd/MM/yyyy HH:mm}");
                text.Span(".");
            });

            column.Item().PaddingTop(60).Row(row =>
            {
                row.RelativeItem().Element(Signature("Firma"));
                row.ConstantItem(40);
                row.RelativeItem().Element(Signature("Aclaración"));
            });
        });
    }

    private static void AddRow(TableDescriptor table, string label, string? value)
    {
        table.Cell().BorderBottom(1).BorderColor(Border).Background("#F8FAFC").Padding(6).Text(label).SemiBold();
        table.Cell().BorderBottom(1).BorderColor(Border).Padding(6).Text(Value(value));
    }

    private static Action<IContainer> Signature(string label) => container =>
        container.BorderTop(1).BorderColor("#0F172A").PaddingTop(4).AlignCenter().Text(label).FontSize(9).FontColor(Muted);

    private static string Value(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    private static string FormatDate(DateTime? date) => date.HasValue ? date.Value.ToString("dd/MM/yyyy", Argentina) : "-";

    private static DateTime ArgentinaNow() => ArgentinaClock.Now;
}
