using Microsoft.Extensions.Logging;
using StudentManagement.Application.DTOs;
using StudentManagement.Application.Interfaces;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Exceptions;
using StudentManagement.Domain.Repositories;

namespace StudentManagement.Application.Services;

internal sealed class ExamService : IExamService
{
    private readonly IExamGradeRepository _examGradeRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly HumanInTheLoopEngine _hitlEngine;
    private readonly ILogger<ExamService> _logger;

    public ExamService(
        IExamGradeRepository examGradeRepository,
        IStudentRepository studentRepository,
        HumanInTheLoopEngine hitlEngine,
        ILogger<ExamService> logger)
    {
        _examGradeRepository = examGradeRepository;
        _studentRepository = studentRepository;
        _hitlEngine = hitlEngine;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExamGradeDto>> GetByStudentAsync(
        Guid studentId, CancellationToken ct = default)
    {
        // Student navigation property Include ile tek sorguda gelir.
        var grades = await _examGradeRepository.GetByStudentIdAsync(studentId, ct);
        return grades.Select(g => Map(g, StudentName(g))).ToList();
    }

    public async Task<IReadOnlyList<ExamGradeDto>> GetFailingAsync(
        decimal threshold = 60, CancellationToken ct = default)
    {
        var allStudents = await _studentRepository.GetAllAsync(ct);

        var failingGrades = new List<ExamGradeDto>();

        foreach (var student in allStudents)
        {
            var grades = await _examGradeRepository.GetByStudentIdAsync(student.Id, ct);

            var failing = grades
                .Where(g => IsFailing(g, threshold))
                .Select(g => Map(g, StudentName(g)));

            failingGrades.AddRange(failing);
        }

        return failingGrades;
    }

    public async Task<UpsertGradeResult> UpsertGradesAsync(
        IReadOnlyList<UpsertGradeRequest> requests, CancellationToken ct = default)
    {
        ValidateGrades(requests);

        var resolution = await _hitlEngine.ResolveStudentsAsync(
            requests,
            r => r.StudentNameOrNumber,
            ocrConfidence: null,
            ct);

        var gradeEntities = resolution.Resolved
            .Select(item => new ExamGrade(
                id: Guid.NewGuid(),
                studentId: item.StudentId,
                courseName: item.Request.CourseName,
                exam1Grade: item.Request.Exam1Grade,
                exam2Grade: item.Request.Exam2Grade))
            .ToList();

        if (gradeEntities.Count > 0)
            await _examGradeRepository.UpsertBatchAsync(gradeEntities, ct);

        _logger.LogInformation(
            "UpsertGrades: {Processed} işlendi, {Skipped} belirsiz.",
            gradeEntities.Count, resolution.Ambiguous.Count);

        var ambiguousItems = resolution.Ambiguous
            .Select(a => new AmbiguousGradeItem(a.OriginalName, a.PossibleMatches, a.Request))
            .ToList();

        return new UpsertGradeResult(
            gradeEntities.Count,
            resolution.NeedsHumanVerification,
            ambiguousItems);
    }

    public async Task UpsertDirectAsync(
        Guid studentId, string courseName, decimal? exam1Grade, decimal? exam2Grade,
        CancellationToken ct = default)
    {
        if (exam1Grade is < 0 or > 100)
            throw new InvalidGradeException($"Sınav1 — {courseName}", exam1Grade!.Value);
        if (exam2Grade is < 0 or > 100)
            throw new InvalidGradeException($"Sınav2 — {courseName}", exam2Grade!.Value);

        var grade = new ExamGrade(
            id: Guid.NewGuid(),
            studentId: studentId,
            courseName: courseName,
            exam1Grade: exam1Grade,
            exam2Grade: exam2Grade);

        await _examGradeRepository.UpsertAsync(grade, ct);
        _logger.LogInformation(
            "UpsertDirect grade: StudentId={StudentId} Course={CourseName}.",
            studentId, courseName);
    }

    public async Task DeleteAsync(Guid studentId, string courseName, CancellationToken ct = default)
    {
        await _examGradeRepository.DeleteAsync(studentId, courseName, ct);
        _logger.LogInformation(
            "Delete grade: StudentId={StudentId} Course={CourseName}.",
            studentId, courseName);
    }

    private static void ValidateGrades(IReadOnlyList<UpsertGradeRequest> requests)
    {
        foreach (var r in requests)
        {
            if (r.Exam1Grade is < 0 or > 100)
                throw new InvalidGradeException(
                    $"Sınav1 — {r.CourseName} / {r.StudentNameOrNumber}",
                    r.Exam1Grade!.Value);

            if (r.Exam2Grade is < 0 or > 100)
                throw new InvalidGradeException(
                    $"Sınav2 — {r.CourseName} / {r.StudentNameOrNumber}",
                    r.Exam2Grade!.Value);
        }
    }

    private static bool IsFailing(ExamGrade g, decimal threshold)
    {
        if (g.Exam1Grade.HasValue && g.Exam1Grade < threshold) return true;
        if (g.Exam2Grade.HasValue && g.Exam2Grade < threshold) return true;
        return false;
    }

    private static string StudentName(ExamGrade g) =>
        g.Student is { } s ? $"{s.FirstName} {s.LastName}" : string.Empty;

    private static ExamGradeDto Map(ExamGrade g, string studentName) =>
        new(g.Id, g.StudentId, studentName, g.CourseName, g.Exam1Grade, g.Exam2Grade);
}
