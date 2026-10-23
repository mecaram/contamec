using System.Globalization;
using ClosedXML.Excel;
using ContaMec.Api.Application.Users.Dto;
using ContaMec.Api.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ContaMec.Api.Application.Users;

/// <summary>Listado de usuarios con los filtros aplicados, para exportar a PDF o Excel.</summary>
public class UserListData
{
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyDocument { get; set; }
    public string? CompanyAddress { get; set; }
    public List<(string Label, string Value)> Filters { get; set; } = [];
    public List<UserResponse> Rows { get; set; } = [];

    /// <summary>Como en pantalla, la columna Empresa solo se muestra al SuperAdministrador.</summary>
    public bool ShowApplication { get; set; }
}

/// <summary>Mismo estilo que los listados de Cuentas, Subcuentas, Ingresos y Egresos.</summary>
public static class UserListReport
{
    private static readonly CultureInfo Argentina = CultureInfo.GetCultureInfo("es-AR");
    private const string Title = "Listado de Usuarios";
    private const string Accent = "#1E3A8A";
    private const string Muted = "#64748B";
    private const string Border = "#CBD5E1";

    public static string FileName(string extension) => $"Listado_Usuarios_{ArgentinaClock.Now:yyyyMMdd_HHmm}.{extension}";

    private static string[] Headers(UserListData data) =>
        data.ShowApplication ? ["ID", "Nombre", "Empresa", "Rol", "Activo"] : ["ID", "Nombre", "Rol", "Activo"];

    private static string[] Values(UserListData data, UserResponse item) =>
        data.ShowApplication
            ? [Value(item.Name), Value(item.ApplicationName), Value(item.UserRoleName)]
            : [Value(item.Name), Value(item.UserRoleName)];

    public static byte[] GeneratePdf(UserListData data)
    {
        var issuedAt = ArgentinaClock.Now;
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
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

    public static byte[] GenerateExcel(UserListData data)
    {
        var issuedAt = ArgentinaClock.Now;
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Usuarios");
        var accent = XLColor.FromHtml(Accent);
        var headers = Headers(data);
        var lastColumn = headers.Length;

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
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(row, i + 1).Value = headers[i];
        var header = sheet.Range(row, 1, row, lastColumn);
        header.Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(accent);

        foreach (var item in data.Rows)
        {
            row++;
            sheet.Cell(row, 1).Value = item.Id;
            var values = Values(data, item);
            for (var i = 0; i < values.Length; i++)
                sheet.Cell(row, i + 2).Value = values[i] == "-" ? string.Empty : values[i];
            sheet.Cell(row, lastColumn).Value = item.IsActive ? "Sí" : "No";
        }

        row++;
        sheet.Cell(row, 1).Value = $"Total registros: {data.Rows.Count}";
        var total = sheet.Range(row, 1, row, lastColumn);
        total.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#EFF6FF"));
        total.Style.Border.SetTopBorder(XLBorderStyleValues.Thin).Border.SetTopBorderColor(accent);

        var table = sheet.Range(headerRow, 1, row, lastColumn);
        table.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetOutsideBorderColor(XLColor.FromHtml(Border));
        sheet.Range(headerRow, lastColumn, row - 1, lastColumn).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        sheet.Range(headerRow, 1, row - 1, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
        if (data.Rows.Count > 0)
            sheet.Range(headerRow, 1, row - 1, lastColumn).SetAutoFilter();

        sheet.SheetView.FreezeRows(headerRow);
        sheet.Columns(1, lastColumn).AdjustToContents(headerRow, row);
        sheet.Column(1).Width = Math.Max(sheet.Column(1).Width, 16);
        sheet.Column(2).Width = Math.Min(Math.Max(sheet.Column(2).Width, 30), 60);
        if (data.ShowApplication)
            sheet.Column(3).Width = Math.Min(Math.Max(sheet.Column(3).Width, 25), 60);
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.SetRowsToRepeatAtTop(headerRow, headerRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void ComposeHeader(IContainer container, UserListData data, DateTime issuedAt)
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

    private static void ComposeFilters(IContainer container, UserListData data)
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
                        columns.ConstantColumn(50);
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

    private static void ComposeTable(IContainer container, UserListData data)
    {
        var headers = Headers(data);
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(50);
                columns.RelativeColumn(3);
                if (data.ShowApplication)
                    columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.ConstantColumn(55);
            });

            table.Header(header =>
            {
                for (var i = 0; i < headers.Length; i++)
                {
                    var cell = header.Cell().Background(Accent).PaddingVertical(5).PaddingHorizontal(4);
                    (i == headers.Length - 1 ? cell.AlignCenter() : cell).Text(headers[i]).FontColor(Colors.White).SemiBold();
                }
            });

            if (data.Rows.Count == 0)
            {
                table.Cell().ColumnSpan((uint)headers.Length).Padding(10).AlignCenter()
                    .Text("No hay usuarios para los filtros seleccionados.").FontColor(Muted);
            }

            for (var index = 0; index < data.Rows.Count; index++)
            {
                var item = data.Rows[index];
                var background = index % 2 == 0 ? "#FFFFFF" : "#F1F5F9";
                IContainer Cell() => table.Cell().Background(background).BorderBottom(0.5f).BorderColor(Border).PaddingVertical(4).PaddingHorizontal(4);

                Cell().Text(item.Id.ToString(Argentina));
                foreach (var value in Values(data, item))
                    Cell().Text(value);
                Cell().AlignCenter().Text(item.IsActive ? "Sí" : "No");
            }

            table.Cell().ColumnSpan((uint)headers.Length).Background("#EFF6FF").BorderTop(1).BorderColor(Accent).Padding(5)
                .Text($"Total registros: {data.Rows.Count}").Bold();
        });
    }

    private static string Value(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
}
