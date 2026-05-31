using Microsoft.Extensions.Logging;
using StudentManagement.Application.DTOs;
using StudentManagement.Application.Interfaces;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Enums;
using StudentManagement.Domain.Repositories;

namespace StudentManagement.Application.Services;

internal sealed class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly HumanInTheLoopEngine _hitlEngine;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IStudentRepository studentRepository,
        HumanInTheLoopEngine hitlEngine,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _studentRepository = studentRepository;
        _hitlEngine = hitlEngine;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InternshipPaymentDto>> GetByStudentAsync(
        Guid studentId, CancellationToken ct = default)
    {
        var student = await _studentRepository.GetByIdAsync(studentId, ct);
        var studentName = student is null ? string.Empty : $"{student.FirstName} {student.LastName}";

        var payments = await _paymentRepository.GetByStudentIdAsync(studentId, ct);
        return payments.Select(p => Map(p, studentName)).ToList();
    }

    public async Task<IReadOnlyList<InternshipPaymentDto>> GetUnpaidAsync(CancellationToken ct = default)
    {
        var payments = await _paymentRepository.GetUnpaidAsync(ct);
        return await EnrichWithStudentNamesAsync(payments, ct);
    }

    public async Task<IReadOnlyList<InternshipPaymentDto>> GetByPeriodAsync(
        int year, int month, CancellationToken ct = default)
    {
        var payments = await _paymentRepository.GetByPeriodAsync(year, month, ct);
        return await EnrichWithStudentNamesAsync(payments, ct);
    }

    public async Task<UpsertPaymentResult> UpsertStudentPaymentsAsync(
        IReadOnlyList<UpsertPaymentRequest> requests, CancellationToken ct = default)
    {
        var resolution = await _hitlEngine.ResolveStudentsAsync(
            requests,
            r => r.StudentNameOrNumber,
            ocrConfidence: null,
            ct);

        var processedCount = 0;
        var skippedCount = 0;

        foreach (var item in resolution.Resolved)
        {
            var status = item.Request.PaymentDate.HasValue ? PaymentStatus.Paid : PaymentStatus.Pending;
            var payment = new InternshipPayment(
                id: Guid.NewGuid(),
                studentId: item.StudentId,
                periodYear: item.Request.PeriodYear,
                periodMonth: item.Request.PeriodMonth,
                amount: item.Request.Amount,
                status: status,
                paymentDate: item.Request.PaymentDate);

            await _paymentRepository.UpsertAsync(payment, ct);
            processedCount++;
        }

        skippedCount = resolution.Ambiguous.Count;

        _logger.LogInformation(
            "UpsertStudentPayments: {Processed} işlendi, {Skipped} belirsiz.",
            processedCount, skippedCount);

        var ambiguousItems = resolution.Ambiguous
            .Select(a => new AmbiguousMatchItem(a.OriginalName, a.PossibleMatches, a.Request))
            .ToList();

        return new UpsertPaymentResult(
            processedCount,
            skippedCount,
            resolution.NeedsHumanVerification,
            ambiguousItems);
    }

    public async Task UpsertDirectAsync(
        Guid studentId, int year, int month, decimal amount, DateOnly? paymentDate,
        int? status = null, CancellationToken ct = default)
    {
        var resolvedStatus = status.HasValue
            ? (PaymentStatus)status.Value
            : (paymentDate.HasValue ? PaymentStatus.Paid : PaymentStatus.Pending);
        var payment = new InternshipPayment(
            id: Guid.NewGuid(),
            studentId: studentId,
            periodYear: year,
            periodMonth: month,
            amount: amount,
            status: resolvedStatus,
            paymentDate: paymentDate);

        await _paymentRepository.UpsertAsync(payment, ct);
        _logger.LogInformation(
            "UpsertDirect payment: StudentId={StudentId} {Year}/{Month} Amount={Amount} Status={Status}.",
            studentId, year, month, amount, resolvedStatus);
    }

    public async Task DeleteAsync(Guid studentId, int year, int month, CancellationToken ct = default)
    {
        await _paymentRepository.DeleteByStudentAndPeriodAsync(studentId, year, month, ct);
        _logger.LogInformation(
            "Delete payment: StudentId={StudentId} {Year}/{Month}.",
            studentId, year, month);
    }

    private async Task<IReadOnlyList<InternshipPaymentDto>> EnrichWithStudentNamesAsync(
        IReadOnlyList<InternshipPayment> payments, CancellationToken ct)
    {
        var studentIds = payments.Select(p => p.StudentId).Distinct().ToList();
        var students = await Task.WhenAll(
            studentIds.Select(id => _studentRepository.GetByIdAsync(id, ct)));

        var nameMap = students
            .Where(s => s is not null)
            .ToDictionary(s => s!.Id, s => $"{s!.FirstName} {s!.LastName}");

        return payments
            .Select(p => Map(p, nameMap.GetValueOrDefault(p.StudentId, string.Empty)))
            .ToList();
    }

    private static InternshipPaymentDto Map(InternshipPayment p, string studentName) =>
        new(p.Id, p.StudentId, studentName, p.PeriodYear, p.PeriodMonth,
            p.Amount, p.PaymentDate, p.Status.ToString());
}
