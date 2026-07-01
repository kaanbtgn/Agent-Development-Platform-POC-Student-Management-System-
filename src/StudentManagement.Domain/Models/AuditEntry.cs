namespace StudentManagement.Domain.Models;

/// <summary>
/// EF Core değişikliklerini MongoDB AuditLogs koleksiyonuna kaydeden denetim kaydı.
/// Tüm değişkenlerin immutable olması KVKK gereği kayıtların değiştirilemez olmasını güvence altına alır.
/// </summary>
public sealed class AuditEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string SessionId { get; init; } = default!;
    public string Action { get; init; } = default!;       // "Create" | "Update" | "Delete"
    public string EntityType { get; init; } = default!;   // "Student" | "InternshipPayment" | "ExamGrade"
    public string EntityId { get; init; } = default!;
    public string? StudentId { get; init; }               // Öğrenciye bağlı kayıtlar için korelasyon alanı
    public string? OldValues { get; init; }               // JSON — hassas alanlar maskelendi
    public string? NewValues { get; init; }               // JSON — hassas alanlar maskelendi
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string Source { get; init; } = "API";          // "API" | "Agent"
}
