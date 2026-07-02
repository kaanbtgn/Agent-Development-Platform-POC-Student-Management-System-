using System.ComponentModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace StudentManagement.MCP.Tools;

[McpServerToolType]
public sealed class StudentTools
{
    private readonly HttpClient _http;

    public StudentTools(IHttpClientFactory factory)
        => _http = factory.CreateClient("StudentManagementApi");

    [McpServerTool]
    [Description("Tüm öğrencileri listeler.")]
    public async Task<string> GetAllStudents(CancellationToken ct)
    {
        var response = await _http.GetAsync("api/students", ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    [McpServerTool]
    [Description("Öğrenci adı veya soyadına göre arama yapar.")]
    public async Task<string> SearchStudents(
        [Description("Aranacak ad veya soyad terimi")]
        string term,
        CancellationToken ct)
    {
        var response = await _http.GetAsync($"api/students/search?term={Uri.EscapeDataString(term)}", ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    [McpServerTool]
    [Description(
        "Sisteme yeni bir öğrenci kaydı ekler. YALNIZCA gerçekten yeni bir öğrenci içindir; " +
        "çağırmadan önce FuzzyMatchStudents ile öğrencinin sistemde zaten olup olmadığını kontrol et " +
        "ve kullanıcıdan 'yeni kayıt oluşturulsun mu?' onayı almadan çağırma. " +
        "Başarılı olursa dönen studentId ile aynı akışta UpsertPayment veya UpsertExamGrade çağrılabilir.")]
    public async Task<string> CreateStudent(
        [Description("Öğrencinin adı")]
        string firstName,
        [Description("Öğrencinin soyadı")]
        string lastName,
        [Description("Öğrenci numarası (benzersiz olmalı; zaten kayıtlıysa hata döner)")]
        string studentNumber,
        [Description("Öğrencinin bölümü")]
        string department,
        [Description("Telefon numarası (yoksa null)")]
        string? phone,
        [Description("Kayıt tarihi (yyyy-MM-dd formatında, örn. 2025-09-01)")]
        string enrollmentDate,
        CancellationToken ct)
    {
        if (!DateOnly.TryParse(enrollmentDate, out var parsedDate))
            return $"Hata: Geçersiz tarih formatı '{enrollmentDate}'. Beklenen format: yyyy-MM-dd (örn. 2025-09-01).";

        var body = new { firstName, lastName, studentNumber, department, phone, enrollmentDate = parsedDate };
        var response = await _http.PostAsJsonAsync("api/students", body, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            var detail = TryExtractProblemDetail(errorBody);
            return detail is not null ? $"Hata: {detail}" : $"Hata: {response.StatusCode}";
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var studentId = doc.RootElement.GetProperty("id").GetGuid();
        return $"Öğrenci oluşturuldu. StudentId: {studentId}";
    }

    [McpServerTool]
    [Description(
        "Öğrenci bilgilerini günceller. " +
        "Güncellemeden önce FuzzyMatchStudents ile doğru öğrencinin bulunduğundan emin ol. " +
        "requiresConfirmation true dönmüşse işlemi kullanıcı onaylamadan gerçekleştirme.")]
    public async Task<string> UpdateStudent(
        [Description("Güncellenecek öğrencinin benzersiz kimliği (UUID)")]
        Guid studentId,
        [Description("Yeni ad (değiştirilmeyecekse null bırak)")]
        string? firstName,
        [Description("Yeni soyad (değiştirilmeyecekse null bırak)")]
        string? lastName,
        [Description("Yeni bölüm (değiştirilmeyecekse null bırak)")]
        string? department,
        [Description("Yeni telefon (değiştirilmeyecekse null bırak)")]
        string? phone,
        CancellationToken ct)
    {
        var body = new { firstName, lastName, department, phone };
        var response = await _http.PutAsJsonAsync($"api/students/{studentId}", body, ct);
        return response.IsSuccessStatusCode ? "Güncelleme başarılı." : $"Hata: {response.StatusCode}";
    }

    [McpServerTool]
    [Description(
        "Öğrenci kaydını siler. " +
        "Bu işlem geri alınamaz; kullanıcıdan açık onay aldıktan sonra çağır.")]
    public async Task<string> DeleteStudent(
        [Description("Silinecek öğrencinin benzersiz kimliği (UUID)")]
        Guid studentId,
        CancellationToken ct)
    {
        var response = await _http.DeleteAsync($"api/students/{studentId}", ct);
        return response.IsSuccessStatusCode ? "Silme başarılı." : $"Hata: {response.StatusCode}";
    }

    [McpServerTool]
    [Description(
        "OCR veya kullanıcı girdisinden gelen ismi doğrudan veritabanında pg_trgm similarity ile arar. " +
        "candidates listesi gerekmez; eşleşme ve skorlama DB tarafında GIN indeksiyle yapılır. " +
        "Sonuçta requiresConfirmation true ise en iyi eşleşmeyi kullanıcıya göster ve onay iste. " +
        "Onay alınmadan UpdateStudent veya DeleteStudent çağırma.")]
    public async Task<string> FuzzyMatchStudents(
        [Description("Eşleştirilecek öğrenci adı")]
        string query,
        [Description("Minimum benzerlik eşiği (0.0–1.0). Varsayılan: 0.3. OCR için düşük tutulmalı.")]
        double threshold = 0.3,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/students/fuzzy-search?q={Uri.EscapeDataString(query)}&threshold={threshold.ToString(CultureInfo.InvariantCulture)}", ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    // API'nin ProblemDetails (application/problem+json) gövdesinden 'detail' alanını okur;
    // örn. DuplicateStudentNumberException mesajını kullanıcıya olduğu gibi iletmek için.
    private static string? TryExtractProblemDetail(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("detail", out var detail) ? detail.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
