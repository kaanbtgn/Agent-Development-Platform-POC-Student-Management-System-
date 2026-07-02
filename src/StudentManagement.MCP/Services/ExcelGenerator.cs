using ClosedXML.Excel;
using StudentManagement.MCP.Models;

namespace StudentManagement.MCP.Services;

internal sealed class ExcelGenerator : IExcelGenerator
{
    private static readonly char[] InvalidSheetNameChars = ['\\', '/', '?', '*', '[', ']', ':'];
    private const int MaxSheetNameLength = 31;

    public (byte[] Content, string FileName, string ContentType) Generate(ExcelDocumentContent content)
    {
        var headers = content.Headers ?? [];
        var rows = content.Rows ?? [];
        var sheetName = SanitizeSheetName(content.SheetName);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);

        // Başlık satırı — koyu, zemin renkli
        for (int col = 0; col < headers.Count; col++)
        {
            var cell = sheet.Cell(1, col + 1);
            cell.Value = headers[col] ?? string.Empty;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
        }

        // Veri satırları
        for (int row = 0; row < rows.Count; row++)
        {
            var rowData = rows[row] ?? [];
            for (int col = 0; col < rowData.Count; col++)
                sheet.Cell(row + 2, col + 1).Value = rowData[col] ?? string.Empty;
        }

        // Sütun genişliklerini içeriğe göre otomatik ayarla
        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        var fileName = $"{Slugify(sheetName)}.xlsx";
        return (ms.ToArray(), fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private static string SanitizeSheetName(string? name)
    {
        var cleaned = string.IsNullOrWhiteSpace(name)
            ? "Sayfa1"
            : string.Concat(name.Split(InvalidSheetNameChars)).Trim();

        if (cleaned.Length == 0)
            cleaned = "Sayfa1";

        return cleaned.Length > MaxSheetNameLength
            ? cleaned[..MaxSheetNameLength].Trim()
            : cleaned;
    }

    private static string Slugify(string name)
    {
        var slug = string.Concat(name.Split(Path.GetInvalidFileNameChars()))
            .Replace(' ', '_')
            .ToLowerInvariant();

        return string.IsNullOrEmpty(slug) ? "belge" : slug;
    }
}

