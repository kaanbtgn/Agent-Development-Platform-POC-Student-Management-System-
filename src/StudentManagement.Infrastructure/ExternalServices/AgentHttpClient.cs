using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StudentManagement.Application.Interfaces;

namespace StudentManagement.Infrastructure.ExternalServices;

/// <summary>
/// API → Agent HTTP köprüsü.
///
/// Akış:
///   1. Session history'yi Redis / MongoDB'den yükler (ChatSessionHistoryService).
///   2. History + message'ı Agent'a gönderir — Agent persistence bilmez.
///      Metin isteği: SSE stream (token-by-token) → SignalR üzerinden frontend'e push edilir.
///      Dosya isteği: tek seferlik yanıt (OCR asenkron süreç nedeniyle streaming desteklenmez).
///   3. Agent'ın döndürdüğü reply'dan user + assistant çiftini oluşturur ve kaydeder.
///   4. Frontend'e temiz JSON döner.
/// </summary>
internal sealed class AgentHttpClient : IAgentClient
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ChatSessionHistoryService _historyService;
    private readonly IRealtimeNotificationService _notification;
    private readonly ILogger<AgentHttpClient> _logger;

    public AgentHttpClient(
        HttpClient http,
        IHttpContextAccessor httpContextAccessor,
        ChatSessionHistoryService historyService,
        IRealtimeNotificationService notification,
        ILogger<AgentHttpClient> logger)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
        _historyService = historyService;
        _notification = notification;
        _logger = logger;
    }

    public async Task<string> ChatAsync(string message, CancellationToken ct = default)
    {
        _logger.LogInformation("Agent streaming chat isteği gönderiliyor.");

        var sessionId = GetSessionId();
        var history = sessionId.Length > 0
            ? await _historyService.LoadAsync(sessionId, ct)
            : [];

        var payload = JsonSerializer.Serialize(new { message, history }, JsonOpts);
        using var requestContent = new StringContent(payload, Encoding.UTF8, "application/json");

        // SSE akışı: ResponseHeadersRead ile body streaming açık kalır
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat/stream")
        {
            Content = requestContent,
        };
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var replyBuilder = new StringBuilder();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream, Encoding.UTF8);

        while (true)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break; // null → stream bitti
            if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]") break;

            var node = JsonNode.Parse(data);
            var token = node?["token"]?.GetValue<string>();
            if (string.IsNullOrEmpty(token)) continue;

            replyBuilder.Append(token);

            if (sessionId.Length > 0)
                await _notification.SendAgentTokenAsync(sessionId, token, ct);
        }

        var reply = replyBuilder.ToString();

        if (sessionId.Length > 0 && reply.Length > 0)
        {
            var entries = new List<HistoryEntry>
            {
                new("user", message),
                new("assistant", reply),
            };
            await _historyService.AppendAsync(sessionId, entries, history, ct);
        }

        // Aynı JSON formatını koru — frontend AgentResponse olarak parse eder
        return JsonSerializer.Serialize(new { reply }, JsonOpts);
    }

    public async Task<string> ChatWithDocumentAsync(
        string message,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Agent'a dosyalı chat isteği gönderiliyor: {FileName}", fileName);

        var sessionId = GetSessionId();
        var history = sessionId.Length > 0
            ? await _historyService.LoadAsync(sessionId, ct)
            : [];

        var historyJson = JsonSerializer.Serialize(history, JsonOpts);

        using var form = new MultipartFormDataContent();

        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);

        form.Add(new StringContent(message, Encoding.UTF8), "message");
        form.Add(new StringContent(historyJson, Encoding.UTF8), "historyJson");

        HttpResponseMessage response = await _http.PostAsync("/api/chat/document", form, ct);
        response.EnsureSuccessStatusCode();

        var rawJson = await response.Content.ReadAsStringAsync(ct);
        return await ParseAndPersistAsync(sessionId, message, history, rawJson, ct);
    }

    /// <summary>
    /// Agent yanıtından <c>reply</c> alanını okur, user + assistant çifti olarak
    /// MongoDB'ye append eder ve Redis sliding window'u günceller.
    /// Yalnızca dosyalı chat (non-streaming) akışı için kullanılır.
    /// </summary>
    private async Task<string> ParseAndPersistAsync(
        string sessionId, string userMessage, List<HistoryEntry> currentHistory, string rawJson, CancellationToken ct)
    {
        var node = JsonNode.Parse(rawJson)?.AsObject();
        if (node is null) return rawJson;

        if (sessionId.Length > 0)
        {
            var reply = node["reply"]?.GetValue<string>() ?? string.Empty;
            if (reply.Length > 0)
            {
                var entries = new List<HistoryEntry>
                {
                    new("user", userMessage),
                    new("assistant", reply),
                };
                await _historyService.AppendAsync(sessionId, entries, currentHistory, ct);
            }
        }

        return node.ToJsonString();
    }

    private string GetSessionId()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return string.Empty;

        if (ctx.Items.TryGetValue("SessionId", out var fromItems) && fromItems is string s && !string.IsNullOrEmpty(s))
            return s;

        return ctx.Request.Headers.TryGetValue("X-Session-Id", out var fromHeader)
            ? fromHeader.ToString()
            : string.Empty;
    }
}
