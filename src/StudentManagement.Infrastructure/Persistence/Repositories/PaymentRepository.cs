using Microsoft.EntityFrameworkCore;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Enums;
using StudentManagement.Domain.Repositories;
using StudentManagement.Infrastructure.Persistence;

namespace StudentManagement.Infrastructure.Persistence.Repositories;

internal sealed class PaymentRepository : IPaymentRepository
{
    private readonly StudentDbContext _context;

    public PaymentRepository(StudentDbContext context) => _context = context;

    public async Task<InternshipPayment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.InternshipPayments.FindAsync([id], ct);

    public async Task<IReadOnlyList<InternshipPayment>> GetByStudentIdAsync(Guid studentId, CancellationToken ct = default)
        => await _context.InternshipPayments
            .AsNoTracking()
            .Where(p => p.StudentId == studentId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<InternshipPayment>> GetByPeriodAsync(int year, int month, CancellationToken ct = default)
        => await _context.InternshipPayments
            .AsNoTracking()
            .Where(p => p.PeriodYear == year && p.PeriodMonth == month)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<InternshipPayment>> GetUnpaidAsync(CancellationToken ct = default)
        => await _context.InternshipPayments
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Overdue)
            .ToListAsync(ct);

    public async Task UpsertAsync(InternshipPayment payment, CancellationToken ct = default)
    {
        var existing = await _context.InternshipPayments
            .SingleOrDefaultAsync(
                p => p.StudentId == payment.StudentId
                  && p.PeriodYear == payment.PeriodYear
                  && p.PeriodMonth == payment.PeriodMonth,
                ct);

        if (existing is null)
        {
            await _context.InternshipPayments.AddAsync(payment, ct);
        }
        else
        {
            existing.Amount = payment.Amount;
            existing.PaymentDate = payment.PaymentDate;
            existing.Status = payment.Status;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteByStudentAndPeriodAsync(Guid studentId, int year, int month, CancellationToken ct = default)
    {
        var existing = await _context.InternshipPayments
            .SingleOrDefaultAsync(
                p => p.StudentId == studentId
                  && p.PeriodYear == year
                  && p.PeriodMonth == month,
                ct);

        if (existing is null)
            return;

        _context.InternshipPayments.Remove(existing);
        await _context.SaveChangesAsync(ct);
    }
}
