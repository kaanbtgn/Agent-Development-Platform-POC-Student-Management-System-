using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Driver;
using StudentManagement.Domain.Models;

namespace StudentManagement.Infrastructure.Audit;

/// <summary>
/// Her <c>SaveChangesAsync</c> çağrısında EF Core değişikliklerini otomatik olarak
/// MongoDB <c>AuditLogs</c> koleksiyonuna kaydeder. Servis koduna müdahale gerekmez.
/// </summary>
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IMongoCollection<AuditEntry> _auditCollection;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Singleton interceptor — DbContext instance başına pending entry listesi tutulur.
    private readonly ConcurrentDictionary<int, List<AuditEntry>> _pending = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AuditInterceptor(
        IMongoCollection<AuditEntry> auditCollection,
        IHttpContextAccessor httpContextAccessor)
    {
        _auditCollection = auditCollection;
        _httpContextAccessor = httpContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            var entries = CollectAuditEntries(eventData.Context);
            if (entries.Count > 0)
                _pending[RuntimeHelpers.GetHashCode(eventData.Context)] = entries;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        int key = eventData.Context is not null
            ? RuntimeHelpers.GetHashCode(eventData.Context)
            : 0;

        if (_pending.TryRemove(key, out var entries) && entries.Count > 0)
            await _auditCollection.InsertManyAsync(entries, cancellationToken: cancellationToken);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        // Kayıt başarısız olursa toplanmış entryleri temizle — yazılmaz.
        if (eventData.Context is not null)
            _pending.TryRemove(RuntimeHelpers.GetHashCode(eventData.Context), out _);

        base.SaveChangesFailed(eventData);
    }

    private List<AuditEntry> CollectAuditEntries(DbContext context)
    {
        var sessionId = _httpContextAccessor.HttpContext?.Items["SessionId"]?.ToString() ?? "system";
        var source = _httpContextAccessor.HttpContext is not null ? "API" : "Agent";
        var entries = new List<AuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var action = entry.State switch
            {
                EntityState.Added   => "Create",
                EntityState.Modified => "Update",
                EntityState.Deleted  => "Delete",
                _                    => null
            };

            if (action is null) continue;

            var entityType = entry.Entity.GetType().Name;
            var entityId   = entry.Properties
                .FirstOrDefault(p => p.Metadata.Name == "Id")
                ?.CurrentValue?.ToString() ?? string.Empty;
            var studentId = ResolveStudentId(entry, entityType, entityId);

            string? oldValues = null;
            string? newValues = null;

            if (entry.State is EntityState.Modified or EntityState.Deleted)
                oldValues = SerializeSnapshot(entry.OriginalValues.Properties
                    .ToDictionary(p => p.Name, p => entry.OriginalValues[p]));

            if (entry.State is EntityState.Added or EntityState.Modified)
                newValues = SerializeSnapshot(entry.CurrentValues.Properties
                    .ToDictionary(p => p.Name, p => entry.CurrentValues[p]));

            entries.Add(new AuditEntry
            {
                SessionId  = sessionId,
                Action     = action,
                EntityType = entityType,
                EntityId   = entityId,
                StudentId  = studentId,
                OldValues  = oldValues,
                NewValues  = newValues,
                Source     = source
            });
        }

        return entries;
    }

    private static string SerializeSnapshot(Dictionary<string, object?> snapshot)
    {
        // Hassas alanları [MASKED] ile değiştir
        var sanitized = snapshot.ToDictionary(
            kv => kv.Key,
            kv => AuditSensitiveFields.Masked.Contains(kv.Key)
                ? (object?)AuditSensitiveFields.MaskedValue
                : kv.Value);

        return JsonSerializer.Serialize(sanitized, _jsonOptions);
    }

    private static string? ResolveStudentId(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        string entityType,
        string entityId)
    {
        if (entityType == "Student")
            return entityId;

        var studentIdProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "StudentId");
        if (studentIdProp is null)
            return null;

        var value = studentIdProp.CurrentValue ?? studentIdProp.OriginalValue;
        return value?.ToString();
    }
}
