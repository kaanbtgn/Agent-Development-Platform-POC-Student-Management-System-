using System.ClientModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using Polly;
using Polly.Retry;
using StudentManagement.Agent.Services.Models;
using StudentManagement.Agent.Services.Prompts;

namespace StudentManagement.Agent.Services;

/// <summary>
/// Saf LLM işlemcisi. Persistence bilgisi yoktur; history API katmanından gelir,
/// güncellenmiş history cevap olarak döner.
/// </summary>
public sealed class StudentManagementAgent
{
    private readonly IChatClient? _chat;
    private readonly AzureDocumentIntelligenceService? _ocr;
    private readonly Lazy<Task<McpClient>> _mcpClientFactory;
    private readonly ILogger<StudentManagementAgent> _logger;

    // MCP araç önbelleği — Singleton ömür boyunca
    private readonly SemaphoreSlim _toolSemaphore = new(1, 1);
    private IList<McpClientTool>? _cachedTools;

    private const int MaxMessagesPerSession = 20;
    private const int MaxLlmRetries = 3;
    private const int DefaultMaxOutputTokens = 8000;
    private readonly int _maxOutputTokens;

    public StudentManagementAgent(
        Lazy<Task<McpClient>> mcpClientFactory,
        ILogger<StudentManagementAgent> logger,
        IChatClient? chat = null,
        AzureDocumentIntelligenceService? ocr = null,
        IConfiguration? configuration = null)
    {
        _chat = chat;
        _ocr = ocr;
        _mcpClientFactory = mcpClientFactory;
        _logger = logger;
        _maxOutputTokens = configuration?.GetValue<int?>("AzureOpenAI:MaxOutputTokens") ?? DefaultMaxOutputTokens;
    }

    public async Task<AgentResponse> ProcessAsync(AgentRequest request, CancellationToken ct)
    {
        // 1. Dosya varsa OCR çalıştır
        string? ocrContent = null;
        OcrMetadata? ocrMetadata = null;

        if (request.File is not null)
        {
            if (_ocr is null)
                return new AgentResponse(
                    Reply: "Dosya analizi için Azure Document Intelligence yapılandırılmamış. appsettings dosyasını kontrol edin.");

            _logger.LogInformation("OCR başlatılıyor: {FileName}", request.File.FileName);
            await using var stream = request.File.OpenReadStream();
            var ocrResult = await _ocr.ParseDocumentAsync(stream, request.File.FileName, ct);
            ocrContent = ocrResult.RawContent;
            ocrMetadata = new OcrMetadata(ocrResult.OverallConfidence, ocrResult.RequiresHumanReview);

            _logger.LogInformation(
                "OCR tamamlandı. Güven: {Confidence:P1}, İnsan onayı: {Review}",
                ocrResult.OverallConfidence, ocrResult.RequiresHumanReview);
        }

        // 2. MCP araçlarını al (ilk çağrıda yükle, sonraki çağrılarda önbellekten döner)
        var tools = await GetCachedToolsAsync(ct);

        // 3. Gelen history'yi ChatMessage listesine dönüştür
        //    System prompt yoksa ekle — Agent kendi prompt'unun sahibi
        var history = request.History
            .Select(e => new ChatMessage(new ChatRole(e.Role), e.Content))
            .ToList();

        if (!history.Any(m => m.Role == ChatRole.System))
            history.Insert(0, new ChatMessage(ChatRole.System, SystemPrompt.Text));

        // OCR içeriği varsa kullanıcı mesajına ekle
        var userText = ocrContent is not null
            ? $"{request.Message}\n\n[Belge İçeriği (OCR)]:\n{ocrContent}"
            : request.Message;

        history.Add(new ChatMessage(ChatRole.User, userText));

        // 4. LLM'e gönder — FunctionInvocationMiddleware tool seçimi ve çağrımı otomatik yapar
        if (_chat is null)
            return new AgentResponse(
                Reply: "Azure OpenAI yapılandırılmamış. appsettings.Development.json dosyasını kontrol edin.",
                OcrMetadata: ocrMetadata);

        _logger.LogInformation("LLM çağrısı başlatılıyor.");

        var options = new ChatOptions
        {
            Tools = [.. tools],
            MaxOutputTokens = _maxOutputTokens,
        };

        var trimmedHistory = TrimHistory(history);
        var retryPolicy = CreateLlmRetryPolicy(ct);
        var completion = await retryPolicy.ExecuteAsync(
            callCt => _chat.GetResponseAsync(trimmedHistory, options, callCt), ct);

        var reply = completion.Text ?? string.Empty;

        _logger.LogInformation("Yanıt üretildi.");

        return new AgentResponse(
            Reply: reply,
            OcrMetadata: ocrMetadata);
    }

    /// <summary>
    /// Metin tabanlı sohbeti LLM'den token-by-token akışı olarak döndürür.
    /// Dosyalı istekler desteklenmez; bu durumda tek parça metin yield edilir.
    /// </summary>
    public async IAsyncEnumerable<string> StreamAsync(
        AgentRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (_chat is null)
        {
            yield return "Azure OpenAI yapılandırılmamış. appsettings.Development.json dosyasını kontrol edin.";
            yield break;
        }

        // Dosya varsa OCR → streaming yerine tek seferde yanıt döner
        if (request.File is not null)
        {
            var full = await ProcessAsync(request, ct);
            yield return full.Reply;
            yield break;
        }

        var tools = await GetCachedToolsAsync(ct);

        var history = request.History
            .Select(e => new ChatMessage(new ChatRole(e.Role), e.Content))
            .ToList();

        if (!history.Any(m => m.Role == ChatRole.System))
            history.Insert(0, new ChatMessage(ChatRole.System, SystemPrompt.Text));

        history.Add(new ChatMessage(ChatRole.User, request.Message));

        var options = new ChatOptions
        {
            Tools = [.. tools],
            MaxOutputTokens = _maxOutputTokens,
        };

        _logger.LogInformation("Streaming LLM çağrısı başlatılıyor.");

        var trimmedHistory = TrimHistory(history);
        var yieldedAny = false;

        for (var attempt = 1; attempt <= MaxLlmRetries; attempt++)
        {
            var shouldRetry = false;
            Exception? pendingError = null;

            await using (var enumerator = _chat.GetStreamingResponseAsync(trimmedHistory, options, ct)
                .GetAsyncEnumerator(ct))
            {
                while (true)
                {
                    bool moved;
                    ChatResponseUpdate? current = null;

                    try
                    {
                        moved = await enumerator.MoveNextAsync();
                        if (moved) current = enumerator.Current;
                    }
                    catch (Exception ex) when (!ct.IsCancellationRequested)
                    {
                        if (!yieldedAny && attempt < MaxLlmRetries && IsTransientLlmError(ex))
                        {
                            pendingError = ex;
                            shouldRetry = true;
                            break;
                        }

                        throw;
                    }

                    if (!moved) yield break;

                    if (!string.IsNullOrEmpty(current!.Text))
                    {
                        yieldedAny = true;
                        yield return current.Text;
                    }
                }
            }

            if (!shouldRetry) yield break;

            _logger.LogWarning(
                pendingError,
                "Streaming LLM çağrısında geçici hata (deneme {Attempt}/{Max}): {Error}",
                attempt, MaxLlmRetries, pendingError?.Message);

            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
        }
    }

    // ── Yardımcı metotlar ────────────────────────────────────────────────

    /// <summary>
    /// LLM çağrılarını (rate limit, 5xx, geçici ağ hatası) en fazla <see cref="MaxLlmRetries"/>
    /// kez, üstel bekleme ile yeniden dener. Kullanıcı isteği iptal ettiyse (cancel butonu)
    /// yeniden denemez — <see cref="OperationCanceledException"/> anında yükselir.
    /// </summary>
    private AsyncRetryPolicy CreateLlmRetryPolicy(CancellationToken ct) =>
        Policy
            .Handle<Exception>(ex => !ct.IsCancellationRequested && IsTransientLlmError(ex))
            .WaitAndRetryAsync(
                retryCount: MaxLlmRetries - 1,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, delay, attempt, _) =>
                    _logger.LogWarning(
                        "LLM çağrısında geçici hata (deneme {Attempt}/{Max}, bekleme {Delay}s): {Error}",
                        attempt, MaxLlmRetries, delay.TotalSeconds, exception.Message));

    private static bool IsTransientLlmError(Exception ex) => ex switch
    {
        ClientResultException cre => cre.Status is 429 or 500 or 502 or 503 or 504,
        HttpRequestException => true,
        TaskCanceledException => true,
        _ => false,
    };

    private async Task<IList<McpClientTool>> GetCachedToolsAsync(CancellationToken ct)
    {
        if (_cachedTools is not null)
            return _cachedTools;

        await _toolSemaphore.WaitAsync(ct);
        try
        {
            if (_cachedTools is null)
            {
                _logger.LogInformation("MCP araçları yükleniyor...");
                var mcpClient = await _mcpClientFactory.Value;
                _cachedTools = await mcpClient.ListToolsAsync(cancellationToken: ct);
                _logger.LogInformation("{Count} MCP aracı yüklendi.", _cachedTools.Count);
            }

            return _cachedTools;
        }
        finally
        {
            _toolSemaphore.Release();
        }
    }

    private static List<ChatMessage> TrimHistory(List<ChatMessage> history)
    {
        if (history.Count <= MaxMessagesPerSession)
            return history;

        // System mesajını koru, en eski user/assistant/tool mesajları at
        var system = history.FirstOrDefault(m => m.Role == ChatRole.System);
        var recent = history
            .Where(m => m.Role != ChatRole.System)
            .TakeLast(MaxMessagesPerSession - (system is null ? 0 : 1))
            .ToList();

        if (system is not null)
            recent.Insert(0, system);

        return recent;
    }
}
