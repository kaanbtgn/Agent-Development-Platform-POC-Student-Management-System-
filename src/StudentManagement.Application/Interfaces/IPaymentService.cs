using StudentManagement.Application.DTOs;

namespace StudentManagement.Application.Interfaces;

public interface IPaymentService
{
    Task<IReadOnlyList<InternshipPaymentDto>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);
    Task<IReadOnlyList<InternshipPaymentDto>> GetUnpaidAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InternshipPaymentDto>> GetByPeriodAsync(int year, int month, CancellationToken ct = default);

    /// <summary>
    /// OCR / Agent batch akışı için — öğrenci adından fuzzy match ile StudentId resolve eder.
    /// </summary>
    Task<UpsertPaymentResult> UpsertStudentPaymentsAsync(
        IReadOnlyList<UpsertPaymentRequest> requests, CancellationToken ct = default);

    /// <summary>
    /// MCP tool'ları için direkt upsert — StudentId zaten bilinmektedir, fuzzy match yapılmaz.
    /// Yalnızca gönderilen (null olmayan) alanlar güncellenir; diğerleri mevcut kayıttan korunur.
    /// </summary>
    Task UpsertDirectAsync(
        Guid studentId, int year, int month, decimal? amount, DateOnly? paymentDate,
        int? status = null, CancellationToken ct = default);

    /// <summary>Belirtilen öğrencinin belirtilen döneme ait ödeme kaydını siler.</summary>
    Task DeleteAsync(Guid studentId, int year, int month, CancellationToken ct = default);
}
