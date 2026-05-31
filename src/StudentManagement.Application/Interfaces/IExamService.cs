using StudentManagement.Application.DTOs;

namespace StudentManagement.Application.Interfaces;

public interface IExamService
{
    Task<IReadOnlyList<ExamGradeDto>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);
    Task<IReadOnlyList<ExamGradeDto>> GetFailingAsync(decimal threshold = 60, CancellationToken ct = default);

    /// <summary>
    /// OCR / Agent batch akışı için — öğrenci adından fuzzy match ile StudentId resolve eder.
    /// </summary>
    Task<UpsertGradeResult> UpsertGradesAsync(
        IReadOnlyList<UpsertGradeRequest> requests, CancellationToken ct = default);

    /// <summary>
    /// MCP tool'ları için direkt upsert — StudentId zaten bilinmektedir, fuzzy match yapılmaz.
    /// </summary>
    Task UpsertDirectAsync(
        Guid studentId, string courseName, decimal? exam1Grade, decimal? exam2Grade,
        CancellationToken ct = default);

    /// <summary>Belirtilen öğrencinin belirtilen derse ait not kaydını siler.</summary>
    Task DeleteAsync(Guid studentId, string courseName, CancellationToken ct = default);
}
