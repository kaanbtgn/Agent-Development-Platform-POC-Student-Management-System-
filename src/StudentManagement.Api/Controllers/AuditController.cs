using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using StudentManagement.Domain.Models;
using StudentManagement.Infrastructure.MongoDB;

namespace StudentManagement.Api.Controllers;

[ApiController]
[Route("api/audit")]
public sealed class AuditController : ControllerBase
{
    private readonly IMongoCollection<AuditEntry> _auditLogs;

    public AuditController(MongoDbContext mongo)
    {
        _auditLogs = mongo.AuditLogs;
    }

    /// <summary>
    /// Belirtilen öğrenciye ait audit geçmişini döner (son 50 kayıt, azalan sıra).
    /// OldValues ve NewValues içindeki hassas alanlar zaten [MASKED] olarak saklanmıştır.
    /// </summary>
    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> GetStudentAuditHistoryAsync(
        Guid studentId, CancellationToken ct)
    {
        var studentIdText = studentId.ToString();
        var filter = Builders<AuditEntry>.Filter.Or(
            Builders<AuditEntry>.Filter.Eq(x => x.StudentId, studentIdText),
            Builders<AuditEntry>.Filter.And(
                Builders<AuditEntry>.Filter.Eq(x => x.EntityType, "Student"),
                Builders<AuditEntry>.Filter.Eq(x => x.EntityId, studentIdText))
        );

        var entries = await _auditLogs
            .Find(filter)
            .SortByDescending(x => x.Timestamp)
            .Limit(50)
            .ToListAsync(ct);

        return Ok(entries);
    }
}
