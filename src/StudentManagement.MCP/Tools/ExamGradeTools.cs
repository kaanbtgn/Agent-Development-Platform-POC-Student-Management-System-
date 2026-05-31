using System.ComponentModel;
using System.Net.Http.Json;
using ModelContextProtocol.Server;

namespace StudentManagement.MCP.Tools;

[McpServerToolType]
public sealed class ExamGradeTools
{
    private readonly HttpClient _http;

    public ExamGradeTools(IHttpClientFactory factory)
        => _http = factory.CreateClient("StudentManagementApi");

    [McpServerTool]
    [Description("Belirtilen öğrencinin tüm sınav notlarını listeler.")]
    public async Task<string> GetExamGrades(
        [Description("Öğrencinin benzersiz kimliği (UUID)")]
        Guid studentId,
        CancellationToken ct)
    {
        var response = await _http.GetAsync($"api/students/{studentId}/exam-grades", ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    [McpServerTool]
    [Description(
        "Öğrencinin belirtilen dersine ait sınav notlarını ekler veya günceller. " +
        "Sadece değiştirilecek notu gönder; diğerini null bırak.")]
    public async Task<string> UpsertExamGrade(
        [Description("Öğrencinin benzersiz kimliği (UUID)")]
        Guid studentId,
        [Description("Ders adı (örn. 'Matematik', 'Fizik')")]
        string courseName,
        [Description("Birinci sınav notu (değiştirilmeyecekse null)")]
        decimal? exam1Grade,
        [Description("İkinci sınav notu (değiştirilmeyecekse null)")]
        decimal? exam2Grade,
        CancellationToken ct)
    {
        var body = new { exam1Grade, exam2Grade };
        var response = await _http.PutAsJsonAsync(
            $"api/students/{studentId}/exam-grades/{Uri.EscapeDataString(courseName)}", body, ct);
        return response.IsSuccessStatusCode ? "Not güncellendi." : $"Hata: {response.StatusCode}";
    }

    [McpServerTool]
    [Description(
        "Öğrencinin belirtilen dersine ait sınav not kaydını kalıcı olarak siler. " +
        "Bu işlem geri alınamaz; kullanıcıdan açık onay aldıktan sonra çağır.")]
    public async Task<string> DeleteExamGrade(
        [Description("Öğrencinin benzersiz kimliği (UUID)")]
        Guid studentId,
        [Description("Silinecek dersin adı (örn. 'Matematik')")]
        string courseName,
        CancellationToken ct)
    {
        var response = await _http.DeleteAsync(
            $"api/students/{studentId}/exam-grades/{Uri.EscapeDataString(courseName)}", ct);
        return response.IsSuccessStatusCode ? "Not silindi." : $"Hata: {response.StatusCode}";
    }
}
