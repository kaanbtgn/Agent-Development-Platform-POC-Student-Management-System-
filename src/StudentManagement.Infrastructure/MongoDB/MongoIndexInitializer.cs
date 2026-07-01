using MongoDB.Driver;
using StudentManagement.Domain.Models;

namespace StudentManagement.Infrastructure.MongoDB;

/// <summary>
/// AuditLogs koleksiyonu için gerekli MongoDB index'lerini tanımlar.
/// Uygulama katmanı bağımlılığı yoktur; doğrudan koleksiyon üzerinde çalışır.
/// </summary>
public static class MongoIndexInitializer
{
    public static async Task EnsureAuditIndexesAsync(IMongoCollection<AuditEntry> collection)
    {
        // EntityId + Timestamp compound index — audit geçmişi sorguları için
        var compoundKey = Builders<AuditEntry>.IndexKeys
            .Ascending(x => x.EntityId)
            .Descending(x => x.Timestamp);

        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<AuditEntry>(
                compoundKey,
                new CreateIndexOptions { Name = "idx_entityId_timestamp", Background = true }));

        // StudentId + Timestamp compound index — öğrenci bazlı tüm entity audit sorguları için
        var studentCompoundKey = Builders<AuditEntry>.IndexKeys
            .Ascending(x => x.StudentId)
            .Descending(x => x.Timestamp);

        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<AuditEntry>(
                studentCompoundKey,
                new CreateIndexOptions { Name = "idx_studentId_timestamp", Background = true }));

        // Timestamp TTL index — 180 gün sonra otomatik sil (KVKK saklama süresi)
        var ttlKey = Builders<AuditEntry>.IndexKeys.Ascending(x => x.Timestamp);

        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<AuditEntry>(
                ttlKey,
                new CreateIndexOptions
                {
                    Name = "idx_timestamp_ttl",
                    ExpireAfter = TimeSpan.FromDays(180),
                    Background = true
                }));
    }
}
