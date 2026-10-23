using System.Globalization;
using ClosedXML.Excel;
using ContaMec.Api.Application.Closures.Dto;
using ContaMec.Api.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ContaMec.Api.Application.Closures;

/// <summary>Listado de cierres con los filtros aplicados, para exportar a PDF o Excel.</summary>
public class ClosureListData
{
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyDocument { get; set; }
    public string? CompanyAddress { get; set; }
    public List<(string Label, string Value)> Filters { get; set; } = [];
    public List<ClosureResponse> Rows { get; set; } = [];
}

/// <summary>Mismo estilo que los demás listados; en A4 apaisado por la cantidad de columnas.</summary>
public static class ClosureListReport
{
    private static readonly CultureInfo Argentina = CultureInfo.GetCultureInfo("es-AR");
    private const string Title = "Listado de Cierres";
    private const string Accent = "#1E3A8A";
    private const string Muted = "#64748B";
    private const string Border = "#CBD5E1";
    private const string MoneyFormat = "\"$\" #,##0.00";

    private static readonly string[] Headers =
        ["N° Cierre", "Apertura", "Saldo Inicial", "Ingresos", "Egresos", "Saldo Final", "Valores en cuenta", "Diferencia", "Cierre", "Estado"];

    // Columnas de importes (base 1): de Saldo Inicial a Diferencia.
    private const int FirstMoneyColumn = 3;
    private const int LastMoneyColumn = 8;

    public static string FileName(string extension) => $"Listado_Cierres_{ArgentinaClock.Now:yyyyMMdd_HHmm}.{extension}";

    // Igual que en pantalla: el total del Saldo Final es Saldo Inicial + Ingresos - Egresos de los totales.
    private static (decimal PreviousBalance, decimal Incomes, decimal Expenses, decimal Result) Totals(ClosureListData data)
    {
        var previousBalance = data.Rows.Sum(r => r.PreviousBalance ?? 0);
        var incomes = data.Rows.Sum(r => r.Incomes);
        var expenses = data.Rows.Sum(r => r.Expenses);
        return (previousBalance, incomes, expenses, previousBalance + incomes - expenses);
    }

    private static decimal Difference(ClosureResponse item) => item.InAccount - (item.Result ?? 0);

    public static byte[] GeneratePdf(ClosureListData data)
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

    public static byte[] GenerateExcel(ClosureListData data)
    {
        var issuedAt = ArgentinaClock.Now;
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Cierres");
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
        sheet.Cell(row, 1).Value = Title.ToUpper(Argentina);
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
            if (item.ClosureNumber.HasValue)
                sheet.Cell(row, 1).Value = item.ClosureNumber.Value;
            if (item.OpenDate.HasValue)
                sheet.Cell(row, 2).Value = item.OpenDate.Value.Date;
            if (item.PreviousBalance.HasValue)
                sheet.Cell(row, 3).Value = item.PreviousBalance.Value;
            sheet.Cell(row, 4).Value = item.Incomes;
            sheet.Cell(row, 5).Value = item.Expenses;
            if (item.Result.HasValue)
                sheet.Cell(row, 6).Value = item.Result.Value;
            sheet.Cell(row, 7).Value = item.InAccount;
            sheet.Cell(row, 8).Value = Difference(item);
            if (item.CloseDate.HasValue)
                sheet.Cell(row, 9).Value = item.CloseDate.Value.Date;
            sheet.Cell(row, 10).Value = Status(item.IsClosed);
        }

        row++;
        var totals = Totals(data);
        sheet.Cell(row, 1).Value = $"Total: {data.Rows.Count}";
        sheet.Cell(row, 3).Value = totals.PreviousBalance;
        sheet.Cell(row, 4).Value = totals.Incomes;
        sheet.Cell(row, 5).Value = totals.Expenses;
        sheet.Cell(row, 6).Value = totals.Result;
        var total = sheet.Range(row, 1, row, lastColumn);
        total.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#EFF6FF"));
        total.Style.Border.SetTopBorder(XLBorderStyleValues.Thin).Border.SetTopBorderColor(accent);

        var table = sheet.Range(headerRow, 1, row, lastColumn);
        table.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetOutsideBorderColor(XLColor.FromHtml(Border));
        sheet.Range(headerRow + 1, FirstMoneyColumn, row, LastMoneyColumn).Style.NumberFormat.Format = MoneyFormat;
        sheet.Range(headerRow + 1, 2, row, 2).Style.DateFormat.Format = "dd/mm/yyyy";
        sheet.Range(headerRow + 1, 9, row, 9).Style.DateFormat.Format = "dd/mm/yyyy";
        sheet.Range(headerRow, FirstMoneyColumn, row, LastMoneyColumn).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        sheet.Range(headerRow, 1, row - 1, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
        sheet.Range(headerRow, 10, row - 1, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        if (data.Rows.Count > 0)
            sheet.Range(headerRow, 1, row - 1, lastColumn).SetAutoFilter();

        sheet.SheetView.FreezeRows(headerRow);
        sheet.Columns(1, lastColumn).AdjustToContents(headerRow, row);
        sheet.Column(1).Width = Math.Max(sheet.Column(1).Width, 20);
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.SetRowsToRepeatAtTop(headerRow, headerRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void ComposeHeader(IContainer container, ClosureListData data, DateTime issuedAt)
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

            row.ConstantItem(200).AlignRight().Column(column =>
            {
                column.Item().AlignRight().Text(Title.ToUpper(Argentina)).FontSize(13).Bold();
                column.Item().AlignRight().Text($"Emitido: {issuedAt:dd/MM/yyyy HH:mm}");
            });
        });
    }

    private static void ComposeFilters(IContainer container, ClosureListData data)
    {
        container.Background("#F8FAFC").Border(1).BorderColor(Border).Padding(8).Column(column =>
        {
            column.Item().PaddingBottom(4).Text("Filtros aplicados").SemiBold().FontColor(Accent);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    for (var i = 0; i < 2; i++)
                    {
                        columns.ConstantColumn(100);
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

    private static void ComposeTable(IContainer container, ClosureListData data)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(50);
                columns.ConstantColumn(55);
                for (var i = FirstMoneyColumn; i <= LastMoneyColumn; i++)
                    columns.RelativeColumn();
                columns.ConstantColumn(55);
                columns.ConstantColumn(50);
            });

            table.Header(header =>
            {
                for (var i = 0; i < Headers.Length; i++)
                {
                    var column = i + 1;
                    var cell = header.Cell().Background(Accent).PaddingVertical(5).PaddingHorizontal(4);
                    var aligned = column >= FirstMoneyColumn && column <= LastMoneyColumn ? cell.AlignRight()
                        : column == Headers.Length ? cell.AlignCenter()
                        : cell;
                    aligned.Text(Headers[i]).FontColor(Colors.White).SemiBold();
                }
            });

            if (data.Rows.Count == 0)
            {
                table.Cell().ColumnSpan((uint)Headers.Length).Padding(10).AlignCenter()
                    .Text("No hay cierres para los filtros seleccionados.").FontColor(Muted);
            }

            for (var index = 0; index < data.Rows.Count; index++)
            {
                var item = data.Rows[index];
                var background = index % 2 == 0 ? "#FFFFFF" : "#F1F5F9";
                IContainer Cell() => table.Cell().Background(background).BorderBottom(0.5f).BorderColor(Border).PaddingVertical(4).PaddingHorizontal(4);

                Cell().Text(item.ClosureNumber?.ToString(Argentina) ?? "-");
                Cell().Text(Date(item.OpenDate));
                Cell().AlignRight().Text(Money(item.PreviousBalance));
                Cell().AlignRight().Text(Money(item.Incomes));
                Cell().AlignRight().Text(Money(item.Expenses));
                Cell().AlignRight().Text(Money(item.Result));
                Cell().AlignRight().Text(Money(item.InAccount));
                Cell().AlignRight().Text(Money(Difference(item)));
                Cell().Text(Date(item.CloseDate));
                Cell().AlignCenter().Text(Status(item.IsClosed));
            }

            var totals = Totals(data);
            IContainer TotalCell() => table.Cell().Background("#EFF6FF").BorderTop(1).BorderColor(Accent).Padding(5);
            TotalCell().Text($"Total: {data.Rows.Count}").Bold();
            TotalCell().Text("-").Bold();
            TotalCell().AlignRight().Text(Money(totals.PreviousBalance)).Bold();
            TotalCell().AlignRight().Text(Money(totals.Incomes)).Bold();
            TotalCell().AlignRight().Text(Money(totals.Expenses)).Bold();
            TotalCell().AlignRight().Text(Money(totals.Result)).Bold().FontColor(Accent);
            table.Cell().ColumnSpan(4).Background("#EFF6FF").BorderTop(1).BorderColor(Accent).Padding(5).Text(string.Empty);
        });
    }

    private static string Status(bool? isClosed) => isClosed == true ? "Cerrado" : "Abierto";

    private static string Date(DateTime? value) => value.HasValue ? value.Value.ToString("dd/MM/yyyy", Argentina) : "-";

    private static string Money(decimal? value) => value.HasValue ? value.Value.ToString("C2", Argentina) : "-";
}
