using Microsoft.AspNetCore.SignalR;
using StudentManagement.Application.Interfaces;

namespace StudentManagement.Infrastructure.Realtime;

public sealed class SignalRNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<AgentHub> _hubContext;

    public SignalRNotificationService(IHubContext<AgentHub> hubContext)
        => _hubContext = hubContext;

    // --- OCR ---

    public Task SendOcrProgressAsync(string sessionId, string step, int progressPercent, CancellationToken ct = default)
        => _hubContext.Clients.Group(sessionId)
            .SendAsync(AgentHubEvents.OcrProgressUpdated, new { step, progressPercent }, ct);

    public Task SendOcrCompletedAsync(string sessionId, string ocrResultJson, CancellationToken ct = default)
        => _hubContext.Clients.Group(sessionId)
            .SendAsync(AgentHubEvents.OcrCompleted, new { result = ocrResultJson }, ct);

    public Task SendOcrFailedAsync(string sessionId, string errorMessage, CancellationToken ct = default)
        => _hubContext.Clients.Group(sessionId)
            .SendAsync(AgentHubEvents.OcrFailed, new { error = errorMessage }, ct);

    // --- Chat / Agent ---

    public Task SendMessageReceivedAsync(string sessionId, string messageId, CancellationToken ct = default)
        => _hubContext.Clients.Group(sessionId)
            .SendAsync(AgentHubEvents.MessageReceived, new { messageId }, ct);

    public Task SendAgentThinkingAsync(string sessionId, CancellationToken ct = default)
        => _hubContext.Clients.Group(sessionId)
            .SendAsync(AgentHubEvents.AgentThinking, new { }, ct);

    public Task SendAgentResponseCompletedAsync(string sessionId, string response, CancellationToken ct = default)
        => _hubContext.Clients.Group(sessionId)
            .SendAsync(AgentHubEvents.AgentResponseCompleted, new { response }, ct);

    public Task SendAgentErrorAsync(string sessionId, string errorMessage, CancellationToken ct = default)
        => _hubContext.Clients.Group(sessionId)
            .SendAsync(AgentHubEvents.AgentError, new { error = errorMessage }, ct);

    public Task SendAgentTokenAsync(string sessionId, string token, CancellationToken ct = default)
        => _hubContext.Clients.Group(sessionId)
            .SendAsync(AgentHubEvents.AgentTokenReceived, new { token }, ct);
}
