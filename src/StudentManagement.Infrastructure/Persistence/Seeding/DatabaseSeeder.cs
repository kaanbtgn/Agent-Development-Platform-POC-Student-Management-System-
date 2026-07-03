using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Enums;
using StudentManagement.Infrastructure.Persistence;

namespace StudentManagement.Infrastructure.Persistence.Seeding;

public static class DatabaseSeeder
{
    /// <summary>
    /// Migration'ları uygular ve isteğe bağlı seed verisini yükler.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider serviceProvider, bool seedData = true, CancellationToken ct = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StudentDbContext>();

        await context.Database.MigrateAsync(ct);

        if (!seedData)
            return;

        if (await context.Students.AnyAsync(ct))
            return;

        var students = BuildStudents();
        await context.Students.AddRangeAsync(students, ct);
        await context.SaveChangesAsync(ct);

        var payments = BuildPayments(students);
        await context.InternshipPayments.AddRangeAsync(payments, ct);
        await context.SaveChangesAsync(ct);

        var grades = BuildGrades(students);
        await context.ExamGrades.AddRangeAsync(grades, ct);
        await context.SaveChangesAsync(ct);
    }

    private static List<Student> BuildStudents() =>
    [
        new(Guid.NewGuid(), "Ali",    "Yılmaz",  "2021001", "Bilgisayar Mühendisliği", new DateOnly(2021, 9, 1)),
        new(Guid.NewGuid(), "Ayşe",   "Kaya",    "2021002", "Elektrik Mühendisliği",   new DateOnly(2021, 9, 1)),
        new(Guid.NewGuid(), "Mehmet", "Demir",   "2022001", "Makine Mühendisliği",     new DateOnly(2022, 9, 1)),
        new(Guid.NewGuid(), "Zeynep", "Çelik",   "2022002", "Endüstri Mühendisliği",   new DateOnly(2022, 9, 1)),
        new(Guid.NewGuid(), "Can",    "Arslan",  "2023001", "Bilgisayar Mühendisliği", new DateOnly(2023, 9, 1)),
    ];

    private static List<InternshipPayment> BuildPayments(List<Student> students)
    {
        var payments = new List<InternshipPayment>();
        var months = new[] { (2025, 10), (2025, 11), (2025, 12) };

        foreach (var student in students)
        {
            foreach (var (year, month) in months)
            {
                payments.Add(new InternshipPayment(
                    id:          Guid.NewGuid(),
                    studentId:   student.Id,
                    periodYear:  year,
                    periodMonth: month,
                    amount:      3500.00m,
                    status:      PaymentStatus.Paid,
                    paymentDate: new DateOnly(year, month, 15)));
            }
        }

        return payments;
    }

    private static List<ExamGrade> BuildGrades(List<Student> students)
    {
        var grades = new List<ExamGrade>();
        var courses = new[]
        {
            ("Algoritma ve Veri Yapıları", 85m, 90m),
            ("Veritabanı Sistemleri",      78m, 82m),
            ("Nesne Yönelimli Programlama",92m, 88m),
            ("Bilgisayar Ağları",          70m, 75m),
            ("İşletim Sistemleri",         88m, 91m),
        };

        foreach (var student in students)
        {
            foreach (var (course, exam1, exam2) in courses)
            {
                grades.Add(new ExamGrade(
                    id:         Guid.NewGuid(),
                    studentId:  student.Id,
                    courseName: course,
                    exam1Grade: exam1,
                    exam2Grade: exam2));
            }
        }

        return grades;
    }
}
