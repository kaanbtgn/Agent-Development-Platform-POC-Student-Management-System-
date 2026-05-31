using StudentManagement.Domain.Entities;

namespace StudentManagement.Domain.Repositories;

/// <summary>
/// Sınav notu verilerine erişim sözleşmesini tanımlar.
/// </summary>
public interface IExamGradeRepository
{
    /// <summary>Belirtilen öğrenciye ait tüm ders not kayıtlarını döndürür.</summary>
    Task<IReadOnlyList<ExamGrade>> GetByStudentIdAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>Kayıt mevcutsa günceller, yoksa ekler (Upsert).</summary>
    Task UpsertAsync(ExamGrade grade, CancellationToken ct = default);

    /// <summary>Birden fazla not kaydını tek seferde upsert eder.</summary>
    Task UpsertBatchAsync(IReadOnlyList<ExamGrade> grades, CancellationToken ct = default);

    /// <summary>Belirtilen öğrencinin belirtilen derse ait not kaydını siler.</summary>
    Task DeleteAsync(Guid studentId, string courseName, CancellationToken ct = default);
}
