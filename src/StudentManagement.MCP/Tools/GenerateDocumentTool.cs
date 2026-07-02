using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using StudentManagement.MCP.Models;
using StudentManagement.MCP.Services;

namespace StudentManagement.MCP.Tools;

[McpServerToolType]
public sealed class GenerateDocumentTool
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IWordGenerator _word;
    private readonly IExcelGenerator _excel;
    private readonly IPdfGenerator _pdf;
    private readonly HttpClient _http;

    public GenerateDocumentTool(
        IWordGenerator word,
        IExcelGenerator excel,
        IPdfGenerator pdf,
        IHttpClientFactory factory)
    {
        _word = word;
        _excel = excel;
        _pdf = pdf;
        _http = factory.CreateClient("StudentManagementApi");
    }

    [McpServerTool(Name = "generate_document")]
    [Description(
        "Kullanıcının istediği tabloyu, raporu veya metni fiziksel bir belge olarak üretir " +
        "ve indirme URL'si döner. " +
        "format: 'Word', 'Excel' veya 'Pdf'. " +
        "Excel için contentJson içinde ExcelDocumentContent şeması (SheetName, Headers, Rows) beklenir. " +
        "Word ve PDF için StandardDocumentContent şeması (Title, Sections) beklenir.")]
    public async Task<string> GenerateDocumentAsync(
        [Description("Çıktı formatı: 'Word', 'Excel' veya 'Pdf' (zorunlu)")]
        string? format = null,

        [Description(
            "Belge içeriğini temsil eden JSON string (zorunlu). " +
            "Excel → {\"sheetName\":\"...\",\"headers\":[...],\"rows\":[[...],...]}, " +
            "Word/Pdf → {\"title\":\"...\",\"sections\":[{\"heading\":\"...\",\"body\":\"...\",\"tables\":[{\"headers\":[...],\"rows\":[[...]]}]}]}")]
        string? contentJson = null,

        CancellationToken ct = default)
    {
        // format/contentJson bilinçli olarak nullable + varsayılan değerli bırakıldı:
        // aksi halde LLM parametreyi atladığında AIFunctionFactory, metot gövdesine hiç
        // girmeden "required parameter missing" hatasıyla isteğin tamamını çökertiyor.
        // Burada yakalayıp LLM'in düzeltebileceği açık bir hata mesajı döndürüyoruz.
        if (string.IsNullOrWhiteSpace(format))
            return "Hata: 'format' parametresi zorunludur. Geçerli değerler: 'Word', 'Excel', 'Pdf'.";

        if (!Enum.TryParse<DocumentOutputFormat>(format, ignoreCase: true, out var parsedFormat))
            return $"Hata: Geçersiz format '{format}'. Geçerli değerler: 'Word', 'Excel', 'Pdf'.";

        if (string.IsNullOrWhiteSpace(contentJson))
            return "Hata: 'contentJson' parametresi zorunludur.";

        byte[] fileBytes;
        string fileName;
        string contentType;

        try
        {
            switch (parsedFormat)
            {
                case DocumentOutputFormat.Excel:
                    var excelContent = JsonSerializer.Deserialize<ExcelDocumentContent>(contentJson, JsonOptions)
                        ?? throw new ArgumentException("contentJson geçerli bir ExcelDocumentContent değil.");
                    (fileBytes, fileName, contentType) = _excel.Generate(excelContent);
                    break;

                case DocumentOutputFormat.Word:
                    var wordContent = JsonSerializer.Deserialize<StandardDocumentContent>(contentJson, JsonOptions)
                        ?? throw new ArgumentException("contentJson geçerli bir StandardDocumentContent değil.");
                    (fileBytes, fileName, contentType) = _word.Generate(wordContent);
                    break;

                case DocumentOutputFormat.Pdf:
                    var pdfContent = JsonSerializer.Deserialize<StandardDocumentContent>(contentJson, JsonOptions)
                        ?? throw new ArgumentException("contentJson geçerli bir StandardDocumentContent değil.");
                    (fileBytes, fileName, contentType) = _pdf.Generate(pdfContent);
                    break;

                default:
                    return $"Hata: Desteklenmeyen format '{format}'.";
            }
        }
        catch (JsonException ex)
        {
            return $"Hata: contentJson geçerli bir JSON değil ({ex.Message}).";
        }
        catch (ArgumentException ex)
        {
            return $"Hata: {ex.Message}";
        }

        // Üretilen dosyayı API'nin iç endpoint'ine yükle
        using var multipart = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        multipart.Add(fileContent, "file", fileName);

        var response = await _http.PostAsync("api/internal/docs/store", multipart, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Serialize(new { status = "error", message = error });
        }

        var result = await response.Content.ReadFromJsonAsync<StoreResult>(cancellationToken: ct);
        return JsonSerializer.Serialize(new { status = "success", downloadUrl = result?.DownloadUrl });
    }

    private record StoreResult(string FileId, string DownloadUrl);
}
