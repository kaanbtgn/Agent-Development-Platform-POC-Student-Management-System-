using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Agent.Services;
using StudentManagement.Agent.Services.Models;

namespace StudentManagement.Agent.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly StudentManagementAgent _agent;
    private readonly ILogger<ChatController> _logger;

    public ChatController(StudentManagementAgent agent, ILogger<ChatController> logger)
    {
        _agent = agent;
        _logger = logger;
    }

    /// <summary>
    /// Dosyasız sohbet — düz JSON.
    /// Body: { "message": "...", "history": [ { "role": "...", "content": "..." } ] }
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    public async Task<IActionResult> Chat(
        [FromBody] ChatRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation("Chat isteği alındı.");

        var agentRequest = new AgentRequest(request.Message, request.History);
        var response = await _agent.ProcessAsync(agentRequest, ct);

        return Ok(response);
    }

    /// <summary>
    /// Dosyalı sohbet — multipart/form-data.
    /// Fields: message (string), historyJson (JSON string), file (IFormFile)
    /// </summary>
    [HttpPost("document")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ChatWithDocument(
        [FromForm] string message,
        [FromForm] string historyJson,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Dosya boş veya eksik.");

        _logger.LogInformation("Dosyalı chat isteği alındı. Dosya: {FileName}", file.FileName);

        var history = JsonSerializer.Deserialize<List<ChatHistoryEntry>>(historyJson, JsonOpts) ?? [];
        var agentRequest = new AgentRequest(message, history, file);
        var response = await _agent.ProcessAsync(agentRequest, ct);

        return Ok(response);
    }

    /// <summary>
    /// Streaming sohbet — Server-Sent Events (text/event-stream).
    /// Her token: <c>data: {"token":"..."}\n\n</c>
    /// Bitiş işareti: <c>data: [DONE]\n\n</c>
    /// </summary>
    [HttpPost("stream")]
    [Consumes("application/json")]
    public async Task ChatStream(
        [FromBody] ChatRequest request,
        CancellationToken ct)
    {
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        var agentRequest = new AgentRequest(request.Message, request.History);

        await foreach (var token in _agent.StreamAsync(agentRequest, ct))
        {
            var json = JsonSerializer.Serialize(new { token }, JsonOpts);
            await Response.WriteAsync($"data: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        await Response.WriteAsync("data: [DONE]\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}

public sealed record ChatRequest(
    string Message,
    List<ChatHistoryEntry> History);
