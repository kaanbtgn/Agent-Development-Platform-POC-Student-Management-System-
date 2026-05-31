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

    // ON CONFLICT ile atomik upsert; EF Core Add/Update kombinasyonundan kaçınılır.
    // Kolon isimleri EF Core + Npgsql'in ürettiği quoted PascalCase ile eşleşmektedir.
    public async Task UpsertAsync(InternshipPayment payment, CancellationToken ct = default)
    {
        await _context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO internship_payments
                ("Id", "StudentId", "PeriodYear", "PeriodMonth", "Amount", "PaymentDate", "Status", "CreatedAt", "UpdatedAt")
            VALUES
                ({payment.Id}, {payment.StudentId}, {payment.PeriodYear}, {payment.PeriodMonth},
                 {payment.Amount}, {payment.PaymentDate}, {(int)payment.Status}, {payment.CreatedAt}, {DateTimeOffset.UtcNow})
            ON CONFLICT ("StudentId", "PeriodYear", "PeriodMonth")
            DO UPDATE SET
                "Amount"      = EXCLUDED."Amount",
                "PaymentDate" = EXCLUDED."PaymentDate",
                "Status"      = EXCLUDED."Status",
                "UpdatedAt"   = EXCLUDED."UpdatedAt"
            """, ct);
    }

    public async Task DeleteByStudentAndPeriodAsync(Guid studentId, int year, int month, CancellationToken ct = default)
        => await _context.Database.ExecuteSqlAsync(
            $"""DELETE FROM internship_payments WHERE "StudentId" = {studentId} AND "PeriodYear" = {year} AND "PeriodMonth" = {month}""", ct);
}
