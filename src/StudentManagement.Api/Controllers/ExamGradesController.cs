using Microsoft.AspNetCore.Mvc;
using StudentManagement.Application.Interfaces;

namespace StudentManagement.Api.Controllers;

[ApiController]
[Route("api/students/{studentId:guid}/exam-grades")]
public sealed class ExamGradesController : ControllerBase
{
    private readonly IExamService _exams;
    private readonly IStudentService _students;

    public ExamGradesController(IExamService exams, IStudentService students)
    {
        _exams = exams;
        _students = students;
    }

    [HttpGet]
    public async Task<IActionResult> GetByStudentAsync(Guid studentId, CancellationToken ct)
    {
        // StudentNotFoundException → GlobalExceptionMiddleware → 404
        await _students.GetByIdAsync(studentId, ct);
        return Ok(await _exams.GetByStudentAsync(studentId, ct));
    }

    /// <summary>
    /// MCP ExamGradeTools'dan direkt çağrılır; studentId agent tarafından önceden resolve edilmiştir.
    /// Fuzzy match süreci bu endpoint öncesinde MCP tarafında tamamlanmış olmalıdır.
    /// </summary>
    [HttpPut("{courseName}")]
    public async Task<IActionResult> UpsertAsync(
        Guid studentId, string courseName,
        [FromBody] ApiUpsertExamGradeRequest req,
        CancellationToken ct)
    {
        await _students.GetByIdAsync(studentId, ct);
        await _exams.UpsertDirectAsync(studentId, courseName, req.Exam1Grade, req.Exam2Grade, ct);
        return NoContent();
    }

    [HttpDelete("{courseName}")]
    public async Task<IActionResult> DeleteAsync(
        Guid studentId, string courseName, CancellationToken ct)
    {
        await _exams.DeleteAsync(studentId, courseName, ct);
        return NoContent();
    }
}

public sealed record ApiUpsertExamGradeRequest(decimal? Exam1Grade, decimal? Exam2Grade);
