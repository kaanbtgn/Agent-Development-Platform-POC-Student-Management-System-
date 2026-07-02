using System.ComponentModel;
using System.Net.Http.Json;
using ModelContextProtocol.Server;

namespace StudentManagement.MCP.Tools;

[McpServerToolType]
public sealed class PaymentTools
{
    private readonly HttpClient _http;

    public PaymentTools(IHttpClientFactory factory)
        => _http = factory.CreateClient("StudentManagementApi");

    [McpServerTool]
    [Description("Belirtilen öğrencinin tüm staj burs ödemelerini listeler.")]
    public async Task<string> GetPayments(
        [Description("Öğrencinin benzersiz kimliği (UUID)")]
        Guid studentId,
        CancellationToken ct)
    {
        var response = await _http.GetAsync($"api/students/{studentId}/payments", ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    [McpServerTool]
    [Description(
        "Belirtilen dönem (yıl/ay) için staj burs ödemesini ekler veya günceller. " +
        "Sadece değiştirilecek alanı gönder; diğerlerini null bırak (mevcut kayıttan korunur). " +
        "Yeni kayıt oluşturuluyorsa amount zorunludur. " +
        "Status değerleri: 0=Pending, 1=Paid, 2=Overdue, 3=Cancelled.")]
    public async Task<string> UpsertPayment(
        [Description("Öğrencinin benzersiz kimliği (UUID)")]
        Guid studentId,
        [Description("Ödeme döneminin yılı (örn. 2025)")]
        int year,
        [Description("Ödeme döneminin ayı (1-12)")]
        int month,
        [Description("Ödeme tutarı (değiştirilmeyecekse null; yeni kayıtta zorunlu)")]
        decimal? amount,
        [Description("Ödeme durumu: 0=Pending, 1=Paid, 2=Overdue, 3=Cancelled (değiştirilmeyecekse null)")]
        int? status,
        [Description("Fiili ödeme tarihi (yyyy-MM-dd formatında, değiştirilmeyecekse null)")]
        string? paymentDate,
        CancellationToken ct)
    {
        DateOnly? parsedDate = null;
        if (paymentDate is not null)
        {
            if (!DateOnly.TryParse(paymentDate, out var parsed))
                return $"Hata: Geçersiz tarih formatı '{paymentDate}'. Beklenen format: yyyy-MM-dd (örn. 2025-06-30).";
            parsedDate = parsed;
        }

        var body = new { amount, status, paymentDate = parsedDate };
        var response = await _http.PutAsJsonAsync(
            $"api/students/{studentId}/payments/{year}/{month}", body, ct);
        return response.IsSuccessStatusCode ? "Ödeme güncellendi." : $"Hata: {response.StatusCode}";
    }

    [McpServerTool]
    [Description(
        "Belirtilen dönem (yıl/ay) için staj burs ödeme kaydını kalıcı olarak siler. " +
        "Bu işlem geri alınamaz; kullanıcıdan açık onay aldıktan sonra çağır.")]
    public async Task<string> DeletePayment(
        [Description("Öğrencinin benzersiz kimliği (UUID)")]
        Guid studentId,
        [Description("Ödeme döneminin yılı (örn. 2025)")]
        int year,
        [Description("Ödeme döneminin ayı (1-12)")]
        int month,
        CancellationToken ct)
    {
        var response = await _http.DeleteAsync(
            $"api/students/{studentId}/payments/{year}/{month}", ct);
        return response.IsSuccessStatusCode ? "Ödeme silindi." : $"Hata: {response.StatusCode}";
    }
}
