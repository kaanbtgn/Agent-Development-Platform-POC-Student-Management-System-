namespace StudentManagement.Application.Interfaces;

public interface IRealtimeNotificationService
{
    // --- OCR ---
    Task SendOcrProgressAsync(string sessionId, string step, int progressPercent, CancellationToken ct = default);
    Task SendOcrCompletedAsync(string sessionId, string ocrResultJson, CancellationToken ct = default);
    Task SendOcrFailedAsync(string sessionId, string errorMessage, CancellationToken ct = default);

    // --- Chat / Agent ---
    /// <summary>Kullanıcı mesajı sunucuya ulaştı, işlem kuyruğa alındı.</summary>
    Task SendMessageReceivedAsync(string sessionId, string messageId, CancellationToken ct = default);

    /// <summary>Agent mesajı aldı ve yanıt üretiyor.</summary>
    Task SendAgentThinkingAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Agent tam yanıtı tamamladı (non-streaming akış).</summary>
    Task SendAgentResponseCompletedAsync(string sessionId, string response, CancellationToken ct = default);

    /// <summary>Agent veya iş akışında kurtarılamaz hata oluştu.</summary>
    Task SendAgentErrorAsync(string sessionId, string errorMessage, CancellationToken ct = default);

    /// <summary>Streaming akışında bir sonraki token geldi.</summary>
    Task SendAgentTokenAsync(string sessionId, string token, CancellationToken ct = default);
}
