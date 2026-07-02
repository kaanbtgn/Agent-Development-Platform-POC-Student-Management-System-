using StudentManagement.Domain.Entities;

namespace StudentManagement.Domain.Repositories;

/// <summary>
/// Staj burs ödeme verilerine erişim sözleşmesini tanımlar.
/// </summary>
public interface IPaymentRepository
{
    /// <summary>Belirtilen kimliğe sahip ödeme kaydını döndürür; bulunamazsa <c>null</c>.</summary>
    Task<InternshipPayment?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Belirtilen öğrenciye ait tüm ödeme kayıtlarını döndürür.</summary>
    Task<IReadOnlyList<InternshipPayment>> GetByStudentIdAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>Belirtilen yıl ve aya ait tüm ödeme kayıtlarını döndürür.</summary>
    Task<IReadOnlyList<InternshipPayment>> GetByPeriodAsync(int year, int month, CancellationToken ct = default);

    /// <summary>Belirtilen öğrenci ve döneme ait ödeme kaydını döndürür; bulunamazsa <c>null</c>.</summary>
    Task<InternshipPayment?> GetByStudentAndPeriodAsync(
        Guid studentId, int year, int month, CancellationToken ct = default);

    /// <summary>Durumu <c>Pending</c> veya <c>Overdue</c> olan tüm ödeme kayıtlarını döndürür.</summary>
    Task<IReadOnlyList<InternshipPayment>> GetUnpaidAsync(CancellationToken ct = default);

    /// <summary>Kayıt mevcutsa günceller, yoksa ekler (Upsert).</summary>
    Task UpsertAsync(InternshipPayment payment, CancellationToken ct = default);

    /// <summary>Belirtilen öğrencinin belirtilen döneme ait ödeme kaydını siler.</summary>
    Task DeleteByStudentAndPeriodAsync(Guid studentId, int year, int month, CancellationToken ct = default);
}
