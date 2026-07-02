using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using StudentManagement.MCP.Models;

namespace StudentManagement.MCP.Services;

internal sealed class WordGenerator : IWordGenerator
{
    public (byte[] Content, string FileName, string ContentType) Generate(StandardDocumentContent content)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body!;

            // Ana başlık
            body.AppendChild(CreateParagraph(content.Title ?? "Belge", "Heading1", true));

            foreach (var section in content.Sections ?? [])
            {
                // Bölüm başlığı
                body.AppendChild(CreateParagraph(section.Heading, "Heading2", true));

                // Bölüm gövde metni
                if (!string.IsNullOrWhiteSpace(section.Body))
                    body.AppendChild(CreateParagraph(section.Body, null, false));

                // Tablolar
                if (section.Tables is { Count: > 0 })
                {
                    foreach (var tableData in section.Tables)
                        body.AppendChild(CreateTable(tableData));
                }
            }

            mainPart.Document.Save();
        }

        var fileName = $"{Slugify(content.Title ?? "belge")}.docx";
        return (ms.ToArray(), fileName, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
    }

    private static Paragraph CreateParagraph(string text, string? styleId, bool bold)
    {
        var run = new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        if (bold)
            run.PrependChild(new RunProperties(new Bold()));

        var para = new Paragraph(run);
        if (styleId is not null)
            para.PrependChild(new ParagraphProperties(new ParagraphStyleId { Val = styleId }));

        return para;
    }

    private static Table CreateTable(DocumentTable tableData)
    {
        var table = new Table();

        // Tablo sınır stili
        table.AppendChild(new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })));

        // Başlık satırı
        table.AppendChild(CreateRow(tableData.Headers ?? [], isHeader: true));

        // Veri satırları
        foreach (var row in tableData.Rows ?? [])
            table.AppendChild(CreateRow(row ?? [], isHeader: false));

        return table;
    }

    private static TableRow CreateRow(IReadOnlyList<string> cells, bool isHeader)
    {
        var row = new TableRow();
        foreach (var cell in cells)
        {
            var run = new Run(new Text(cell ?? string.Empty) { Space = SpaceProcessingModeValues.Preserve });
            if (isHeader)
                run.PrependChild(new RunProperties(new Bold()));

            row.AppendChild(new TableCell(new Paragraph(run)));
        }
        return row;
    }

    private static string Slugify(string title)
        => string.Concat(title.Split(Path.GetInvalidFileNameChars()))
                 .Replace(' ', '_')
                 .ToLowerInvariant();
}
