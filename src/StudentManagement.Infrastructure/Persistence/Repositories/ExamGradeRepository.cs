using Microsoft.EntityFrameworkCore;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Repositories;
using StudentManagement.Infrastructure.Persistence;

namespace StudentManagement.Infrastructure.Persistence.Repositories;

internal sealed class ExamGradeRepository : IExamGradeRepository
{
    private readonly StudentDbContext _context;

    public ExamGradeRepository(StudentDbContext context) => _context = context;

    public async Task<IReadOnlyList<ExamGrade>> GetByStudentIdAsync(Guid studentId, CancellationToken ct = default)
        => await _context.ExamGrades
            .AsNoTracking()
            .Include(g => g.Student)
            .Where(g => g.StudentId == studentId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ExamGrade>> GetFailingAsync(decimal threshold, CancellationToken ct = default)
        => await _context.ExamGrades
            .AsNoTracking()
            .Include(g => g.Student)
            .Where(g => (g.Exam1Grade.HasValue && g.Exam1Grade < threshold)
                     || (g.Exam2Grade.HasValue && g.Exam2Grade < threshold))
            .ToListAsync(ct);

    public async Task UpsertAsync(ExamGrade grade, CancellationToken ct = default)
        => await UpsertCoreAsync(grade, ct);

    // Başarısız bir kayıt tüm batch'i geri alır
    public async Task UpsertBatchAsync(IReadOnlyList<ExamGrade> grades, CancellationToken ct = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var grade in grades)
                await UpsertCoreAsync(grade, ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task UpsertCoreAsync(ExamGrade grade, CancellationToken ct)
    {
        await _context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO exam_grades
                ("Id", "StudentId", "CourseName", "Exam1Grade", "Exam2Grade", "CreatedAt", "UpdatedAt")
            VALUES
                ({grade.Id}, {grade.StudentId}, {grade.CourseName},
                 {grade.Exam1Grade}, {grade.Exam2Grade}, {grade.CreatedAt}, {DateTimeOffset.UtcNow})
            ON CONFLICT ("StudentId", "CourseName")
            DO UPDATE SET
                "Exam1Grade" = EXCLUDED."Exam1Grade",
                "Exam2Grade" = EXCLUDED."Exam2Grade",
                "UpdatedAt"  = EXCLUDED."UpdatedAt"
            """, ct);
    }

    public async Task DeleteAsync(Guid studentId, string courseName, CancellationToken ct = default)
        => await _context.Database.ExecuteSqlAsync(
            $"""DELETE FROM exam_grades WHERE "StudentId" = {studentId} AND "CourseName" = {courseName}""", ct);
}
