using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudentManagement.Application.DTOs;
using StudentManagement.Application.Interfaces;
using StudentManagement.Application.Services;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Exceptions;
using StudentManagement.Domain.Repositories;

namespace StudentManagement.UnitTests.Application;

public sealed class ExamServiceTests
{
    private readonly Mock<IExamGradeRepository> _gradeRepo = new();
    private readonly Mock<IStudentRepository> _studentRepo = new();
    private readonly Mock<IFuzzyMatcher> _fuzzyMatcher = new();

    private HumanInTheLoopEngine BuildEngine() =>
        new(_studentRepo.Object,
            _fuzzyMatcher.Object,
            NullLogger<HumanInTheLoopEngine>.Instance);

    private ExamService BuildSut() =>
        new(_gradeRepo.Object,
            BuildEngine(),
            NullLogger<ExamService>.Instance);

    // ── Scenario 1: Exam1Grade > 100 ─────────────────────────────────────────

    [Fact]
    public async Task UpsertGradesAsync_GradeExceedsMaxGrade_ThrowsInvalidGradeException()
    {
        var request = new UpsertGradeRequest("Ali Veli", "Matematik", Exam1Grade: 110m, Exam2Grade: 80m);

        var act = () => BuildSut().UpsertGradesAsync([request]);

        await act.Should().ThrowAsync<InvalidGradeException>();
    }

    // ── Scenario 2: Negative grade ────────────────────────────────────────────

    [Fact]
    public async Task UpsertGradesAsync_NegativeGrade_ThrowsInvalidGradeException()
    {
        var request = new UpsertGradeRequest("Ali Veli", "Fizik", Exam1Grade: -5m, Exam2Grade: 70m);

        var act = () => BuildSut().UpsertGradesAsync([request]);

        await act.Should().ThrowAsync<InvalidGradeException>();
    }

    // ── Scenario 3: Valid grades → ProcessedCount = 3 ────────────────────────

    [Fact]
    public async Task UpsertGradesAsync_ValidGrades_ProcessesSuccessfully()
    {
        var requests = new[]
        {
            new UpsertGradeRequest("Ali Veli",   "Matematik", 85m, 90m),
            new UpsertGradeRequest("Ayşe Kaya",  "Fizik",     70m, 75m),
            new UpsertGradeRequest("Mehmet Can", "Kimya",     95m, 88m),
        };

        var students = requests
            .Select(r => MakeStudentForName(r.StudentNameOrNumber))
            .ToArray();

        for (int i = 0; i < requests.Length; i++)
        {
            var name = requests[i].StudentNameOrNumber;
            var student = students[i];

            _studentRepo.Setup(r => r.GetByStudentNumberAsync(name, default))
                        .ReturnsAsync((Student?)null);
            _studentRepo.Setup(r => r.SearchByNameAsync(name, default))
                        .ReturnsAsync([student]);

            var fullName = $"{student.FirstName} {student.LastName}";
            _fuzzyMatcher
                .Setup(f => f.FindBestMatches(name, It.IsAny<IEnumerable<string>>(), It.IsAny<double>()))
                .Returns([new FuzzyMatch(fullName, 0.95)]);
        }

        _gradeRepo.Setup(r => r.UpsertBatchAsync(It.IsAny<IReadOnlyList<ExamGrade>>(), default))
                  .Returns(Task.CompletedTask);

        var result = await BuildSut().UpsertGradesAsync(requests);

        result.ProcessedCount.Should().Be(3);
        result.NeedsHumanVerification.Should().BeFalse();
        _gradeRepo.Verify(r => r.UpsertBatchAsync(
            It.Is<IReadOnlyList<ExamGrade>>(g => g.Count == 3), default), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Student MakeStudentForName(string fullName)
    {
        var parts = fullName.Split(' ', 2);
        var id = Guid.NewGuid();
        return new Student(id, parts[0], parts.Length > 1 ? parts[1] : "X",
            studentNumber: $"STU{id.ToString()[..4].ToUpper()}",
            department: "Test",
            enrollmentDate: DateOnly.FromDateTime(DateTime.Today));
    }
}
