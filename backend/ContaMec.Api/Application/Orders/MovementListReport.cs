using System.Globalization;
using ClosedXML.Excel;
using ContaMec.Api.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ContaMec.Api.Application.Orders;

public enum ListExportFormat
{
    Pdf,
    Excel
}

public class MovementListRow
{
    public int Id { get; set; }
    public DateTime? EmissionDate { get; set; }
    public string? AccountName { get; set; }
    public string? SubAccountName { get; set; }
    public string? OccurrenceType { get; set; }
    public string? Detail { get; set; }
    public decimal? Amount { get; set; }
    public string? PaymentAccountName { get; set; }
}

/// <summary>Listado de ingresos o egresos con los filtros aplicados, para exportar a PDF o Excel.</summary>
public class MovementListData
{
    public OrderKind Kind { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyDocument { get; set; }
    public string? CompanyAddress { get; set; }
    public List<(string Label, string Value)> Filters { get; set; } = [];
    public List<MovementListRow> Rows { get; set; } = [];
}

/// <summary>Mismo estilo que la Orden de Ingreso y de Egreso.</summary>
public static class MovementListReport
{
    private static readonly CultureInfo Argentina = CultureInfo.GetCultureInfo("es-AR");
    private const string Accent = "#1E3A8A";
    private const string Muted = "#64748B";
    private const string Border = "#CBD5E1";

    private static readonly string[] Headers = ["N.º", "Fecha", "Cuenta", "Subcuenta", "Ocurrencia", "Detalle", "Cuenta de pago", "Importe"];

    public static string Title(OrderKind kind) => kind == OrderKind.Income ? "Listado de Ingresos" : "Listado de Egresos";

    public static string FileName(OrderKind kind, string extension) =>
        $"{(kind == OrderKind.Income ? "Listado_Ingresos" : "Listado_Egresos")}_{ArgentinaClock.Now:yyyyMMdd_HHmm}.{extension}";

    public static byte[] GeneratePdf(MovementListData data)
    {
        var issuedAt = ArgentinaClock.Now;
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor("#0F172A"));

                page.Header().Element(c => ComposeHeader(c, data, issuedAt));
                page.Content().PaddingVertical(12).Column(column =>
                {
                    column.Spacing(12);
                    column.Item().Element(c => ComposeFilters(c, data));
                    column.Item().Element(c => ComposeTable(c, data));
                });
                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text($"Generado con ContaMEC el {issuedAt:dd/MM/yyyy HH:mm}").FontSize(7.5f).FontColor(Muted);
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(7.5f).FontColor(Muted));
                        text.Span("Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerateExcel(MovementListData data)
    {
        var issuedAt = ArgentinaClock.Now;
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(data.Kind == OrderKind.Income ? "Ingresos" : "Egresos");
        var accent = XLColor.FromHtml(Accent);
        var lastColumn = Headers.Length;

        var row = 1;
        sheet.Cell(row, 1).Value = data.CompanyName;
        sheet.Cell(row, 1).Style.Font.SetBold().Font.SetFontSize(15).Font.SetFontColor(accent);
        foreach (var line in new[] { data.CompanyDocument, data.CompanyAddress }.Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            row++;
            sheet.Cell(row, 1).Value = line;
            sheet.Cell(row, 1).Style.Font.SetFontColor(XLColor.FromHtml(Muted));
        }

        row += 2;
        sheet.Cell(row, 1).Value = Title(data.Kind).ToUpper(Argentina);
        sheet.Cell(row, 1).Style.Font.SetBold().Font.SetFontSize(13);
        sheet.Cell(row, lastColumn).Value = $"Emitido: {issuedAt:dd/MM/yyyy HH:mm}";
        sheet.Cell(row, lastColumn).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

        row += 2;
        sheet.Cell(row, 1).Value = "Filtros aplicados";
        sheet.Cell(row, 1).Style.Font.SetBold().Font.SetFontColor(accent);
        foreach (var (label, value) in data.Filters)
        {
            row++;
            sheet.Cell(row, 1).Value = label;
            sheet.Cell(row, 1).Style.Font.SetBold();
            sheet.Cell(row, 2).Value = value;
        }

        row += 2;
        var headerRow = row;
        for (var i = 0; i < Headers.Length; i++)
            sheet.Cell(row, i + 1).Value = Headers[i];
        var header = sheet.Range(row, 1, row, lastColumn);
        header.Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(accent);

        foreach (var item in data.Rows)
        {
            row++;
            sheet.Cell(row, 1).Value = item.Id;
            if (item.EmissionDate.HasValue)
                sheet.Cell(row, 2).Value = item.EmissionDate.Value.Date;
            sheet.Cell(row, 3).Value = item.AccountName ?? string.Empty;
            sheet.Cell(row, 4).Value = item.SubAccountName ?? string.Empty;
            sheet.Cell(row, 5).Value = item.OccurrenceType ?? string.Empty;
            sheet.Cell(row, 6).Value = item.Detail ?? string.Empty;
            sheet.Cell(row, 7).Value = item.PaymentAccountName ?? string.Empty;
            if (item.Amount.HasValue)
                sheet.Cell(row, 8).Value = item.Amount.Value;
        }

        row++;
        sheet.Cell(row, 1).Value = $"Total registros: {data.Rows.Count}";
        sheet.Cell(row, 7).Value = "Total";
        sheet.Cell(row, 8).Value = data.Rows.Sum(r => r.Amount ?? 0);
        var total = sheet.Range(row, 1, row, lastColumn);
        total.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#EFF6FF"));
        total.Style.Border.SetTopBorder(XLBorderStyleValues.Thin).Border.SetTopBorderColor(accent);

        var table = sheet.Range(headerRow, 1, row, lastColumn);
        table.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetOutsideBorderColor(XLColor.FromHtml(Border));
        if (data.Rows.Count > 0)
            sheet.Range(headerRow + 1, 1, row - 1, 1).Style.NumberFormat.Format = "00000000";
        sheet.Range(headerRow + 1, 2, row, 2).Style.DateFormat.Format = "dd/mm/yyyy";
        sheet.Range(headerRow + 1, 8, row, 8).Style.NumberFormat.Format = "\"$\" #,##0.00";
        sheet.Range(headerRow, 7, row, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        sheet.Cell(headerRow, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
        sheet.Range(headerRow + 1, 7, row - 1, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
        if (data.Rows.Count > 0)
            sheet.Range(headerRow, 1, row - 1, lastColumn).SetAutoFilter();

        sheet.SheetView.FreezeRows(headerRow);
        sheet.Columns(1, lastColumn).AdjustToContents(headerRow, row);
        sheet.Column(1).Width = Math.Max(sheet.Column(1).Width, 16);
        sheet.Column(6).Width = Math.Min(Math.Max(sheet.Column(6).Width, 30), 60);
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.SetRowsToRepeatAtTop(headerRow, headerRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void ComposeHeader(IContainer container, MovementListData data, DateTime issuedAt)
    {
        container.BorderBottom(1.5f).BorderColor(Accent).PaddingBottom(8).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(data.CompanyName).FontSize(14).Bold().FontColor(Accent);
                if (!string.IsNullOrWhiteSpace(data.CompanyDocument))
                    column.Item().Text(data.CompanyDocument).FontColor(Muted);
                if (!string.IsNullOrWhiteSpace(data.CompanyAddress))
                    column.Item().Text(data.CompanyAddress).FontColor(Muted);
            });

            row.ConstantItem(220).AlignRight().Column(column =>
            {
                column.Item().AlignRight().Text(Title(data.Kind).ToUpper(Argentina)).FontSize(13).Bold();
                column.Item().AlignRight().Text($"Emitido: {issuedAt:dd/MM/yyyy HH:mm}");
            });
        });
    }

    private static void ComposeFilters(IContainer container, MovementListData data)
    {
        container.Background("#F8FAFC").Border(1).BorderColor(Border).Padding(8).Column(column =>
        {
            column.Item().PaddingBottom(4).Text("Filtros aplicados").SemiBold().FontColor(Accent);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    for (var i = 0; i < 3; i++)
                    {
                        columns.ConstantColumn(80);
                        columns.RelativeColumn();
                    }
                });

                foreach (var (label, value) in data.Filters)
                {
                    table.Cell().PaddingVertical(1.5f).Text(label).SemiBold();
                    table.Cell().PaddingVertical(1.5f).PaddingRight(8).Text(value);
                }
            });
        });
    }

    private static void ComposeTable(IContainer container, MovementListData data)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(58);
                columns.ConstantColumn(56);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.ConstantColumn(66);
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.ConstantColumn(82);
            });

            table.Header(header =>
            {
                for (var i = 0; i < Headers.Length; i++)
                {
                    var cell = header.Cell().Background(Accent).PaddingVertical(5).PaddingHorizontal(4);
                    (i == Headers.Length - 1 ? cell.AlignRight() : cell).Text(Headers[i]).FontColor(Colors.White).SemiBold();
                }
            });

            if (data.Rows.Count == 0)
            {
                table.Cell().ColumnSpan((uint)Headers.Length).Padding(10).AlignCenter()
                    .Text("No hay registros para los filtros seleccionados.").FontColor(Muted);
            }

            for (var index = 0; index < data.Rows.Count; index++)
            {
                var item = data.Rows[index];
                var background = index % 2 == 0 ? "#FFFFFF" : "#F1F5F9";
                IContainer Cell() => table.Cell().Background(background).BorderBottom(0.5f).BorderColor(Border).PaddingVertical(4).PaddingHorizontal(4);

                Cell().Text(OrderPdf.FormatNumber(item.Id));
                Cell().Text(item.EmissionDate.HasValue ? item.EmissionDate.Value.ToString("dd/MM/yyyy", Argentina) : "-");
                Cell().Text(Value(item.AccountName));
                Cell().Text(Value(item.SubAccountName));
                Cell().Text(Value(item.OccurrenceType));
                Cell().Text(Value(item.Detail));
                Cell().Text(Value(item.PaymentAccountName));
                Cell().AlignRight().Text(item.Amount.HasValue ? item.Amount.Value.ToString("C2", Argentina) : "-");
            }

            // Al final del listado, no en cada página: el total es el de todos los registros.
            table.Cell().ColumnSpan(5).Background("#EFF6FF").BorderTop(1).BorderColor(Accent).Padding(5)
                .Text($"Total registros: {data.Rows.Count}").Bold();
            table.Cell().ColumnSpan(2).Background("#EFF6FF").BorderTop(1).BorderColor(Accent).Padding(5).AlignRight()
                .Text("Total").Bold();
            table.Cell().Background("#EFF6FF").BorderTop(1).BorderColor(Accent).Padding(5).AlignRight()
                .Text(data.Rows.Sum(r => r.Amount ?? 0).ToString("C2", Argentina)).Bold().FontColor(Accent);
        });
    }

    private static string Value(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
}
