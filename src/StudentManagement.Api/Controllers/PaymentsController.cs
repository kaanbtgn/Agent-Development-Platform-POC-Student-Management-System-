using Microsoft.AspNetCore.Mvc;
using StudentManagement.Application.Interfaces;

namespace StudentManagement.Api.Controllers;

[ApiController]
[Route("api/students/{studentId:guid}/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly IStudentService _students;

    public PaymentsController(IPaymentService payments, IStudentService students)
    {
        _payments = payments;
        _students = students;
    }

    [HttpGet]
    public async Task<IActionResult> GetByStudentAsync(Guid studentId, CancellationToken ct)
    {
        // StudentNotFoundException → GlobalExceptionMiddleware → 404
        await _students.GetByIdAsync(studentId, ct);
        return Ok(await _payments.GetByStudentAsync(studentId, ct));
    }

    /// <summary>
    /// MCP PaymentTools'dan direkt çağrılır; studentId agent tarafından önceden resolve edilmiştir.
    /// Fuzzy match süreci bu endpoint öncesinde MCP tarafında tamamlanmış olmalıdır.
    /// </summary>
    [HttpPut("{year:int}/{month:int}")]
    public async Task<IActionResult> UpsertAsync(
        Guid studentId, int year, int month,
        [FromBody] ApiUpsertPaymentRequest req,
        CancellationToken ct)
    {
        await _students.GetByIdAsync(studentId, ct);
        await _payments.UpsertDirectAsync(studentId, year, month, req.Amount, req.PaymentDate, req.Status, ct);
        return NoContent();
    }

    [HttpDelete("{year:int}/{month:int}")]
    public async Task<IActionResult> DeleteAsync(
        Guid studentId, int year, int month, CancellationToken ct)
    {
        await _payments.DeleteAsync(studentId, year, month, ct);
        return NoContent();
    }
}

public sealed record ApiUpsertPaymentRequest(decimal? Amount, DateOnly? PaymentDate, int? Status = null);
