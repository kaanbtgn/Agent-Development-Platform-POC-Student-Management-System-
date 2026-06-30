using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StudentManagement.MCP.Models;

namespace StudentManagement.MCP.Services;

internal sealed class PdfGenerator : IPdfGenerator
{
    public (byte[] Content, string FileName, string ContentType) Generate(StandardDocumentContent content)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Text(content.Title)
                    .SemiBold().FontSize(18).FontColor(Colors.Grey.Darken3);

                page.Content().Column(col =>
                {
                    col.Spacing(12);

                    foreach (var section in content.Sections)
                    {
                        // Bölüm başlığı
                        col.Item().Text(section.Heading)
                            .Bold().FontSize(13).FontColor(Colors.Blue.Darken2);

                        // Bölüm gövdesi
                        if (!string.IsNullOrWhiteSpace(section.Body))
                            col.Item().Text(section.Body);

                        // Tablolar
                        if (section.Tables is { Count: > 0 })
                        {
                            foreach (var tableData in section.Tables)
                            {
                                col.Item().Table(table =>
                                {
                                    // Sütun genişlikleri eşit paylaştırılır
                                    table.ColumnsDefinition(def =>
                                    {
                                        foreach (var _ in tableData.Headers)
                                            def.RelativeColumn();
                                    });

                                    // Başlık satırı — tüm hücreler tek bir Header() callback içinde tanımlanmalı
                                    table.Header(h =>
                                    {
                                        foreach (var header in tableData.Headers)
                                            h.Cell().Background(Colors.Blue.Lighten4)
                                                .Padding(4)
                                                .Text(header).Bold();
                                    });

                                    // Veri satırları
                                    bool alternate = false;
                                    foreach (var row in tableData.Rows)
                                    {
                                        var bg = alternate ? Colors.Grey.Lighten4 : Colors.White;
                                        foreach (var cell in row)
                                        {
                                            table.Cell().Background(bg)
                                                .Border(1).BorderColor(Colors.Grey.Lighten2)
                                                .Padding(4)
                                                .Text(cell ?? string.Empty);
                                        }
                                        alternate = !alternate;
                                    }
                                });
                            }
                        }
                    }
                });

                page.Footer().AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Sayfa ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
            });
        });

        var bytes = document.GeneratePdf();
        var fileName = $"{Slugify(content.Title)}.pdf";
        return (bytes, fileName, "application/pdf");
    }

    private static string Slugify(string title)
        => string.Concat(title.Split(Path.GetInvalidFileNameChars()))
                 .Replace(' ', '_')
                 .ToLowerInvariant();
}
