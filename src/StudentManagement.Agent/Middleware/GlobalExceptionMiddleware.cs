using Microsoft.AspNetCore.Mvc;

namespace StudentManagement.Agent.Middleware;

/// <summary>
/// İstemci (Api) bağlantıyı iptal ettiğinde (örn. kullanıcı "İptal Et" butonuna
/// bastığında) veya beklenmeyen bir hata oluştuğunda temiz bir yanıt döner.
/// StudentManagement.Api'deki GlobalExceptionMiddleware ile aynı desen —
/// tek fark: <c>ChatStream</c> gibi gerçek SSE akışlarında yanıt zaten başlamış
/// olabileceğinden StatusCode ataması <c>HasStarted</c> kontrolüyle korunuyor.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // İstemci bağlantıyı kapattı (cancel butonu / sayfa kapatma) — loglama gerekmez.
            // SSE akışı (ChatStream) zaten yanıt yazmaya başlamış olabilir; bu durumda
            // StatusCode atamak InvalidOperationException fırlatır, o yüzden korumalı.
            if (!context.Response.HasStarted)
                context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlenmeyen exception: {Message}", ex.Message);

            if (context.Response.HasStarted) return;

            var detail = _env.IsDevelopment() ? ex.ToString() : null;
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "Beklenmeyen bir hata oluştu.",
                detail);
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string? exceptionDetail = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (exceptionDetail is not null)
            problem.Extensions["exception"] = exceptionDetail;

        await context.Response.WriteAsJsonAsync(problem);
    }
}
