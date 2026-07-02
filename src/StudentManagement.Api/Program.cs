using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using NLog;
using NLog.Web;
using StudentManagement.Api.BackgroundServices;
using StudentManagement.Api.Middleware;
using StudentManagement.Api.Models;
using StudentManagement.Application;
using StudentManagement.Infrastructure;
using StudentManagement.Infrastructure.Persistence.Seeding;
using StudentManagement.Infrastructure.Realtime;
using System.Threading.Channels;
using System.Threading.RateLimiting;

// NLog'u erken başlat — startup logları da yakalanır
var logger = LogManager.Setup()
    .LoadConfigurationFromFile("nlog.config")
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)   // AddSignalR + IRealtimeNotificationService burada
        .AddHttpContextAccessor()
        .AddControllers();

    // OCR arka plan işleme — Channel tabanlı kuyruk
    builder.Services.AddSingleton(_ => Channel.CreateBounded<OcrJob>(new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true
    }));
    builder.Services.AddHostedService<OcrBackgroundService>();

    builder.Services
        .AddCors(options => options.AddPolicy("FrontendPolicy", policy =>
        {
            var allowedOrigin = builder.Configuration["AllowedOrigin"];
            if (!string.IsNullOrWhiteSpace(allowedOrigin) && allowedOrigin != "*")
            {
                policy.WithOrigins(allowedOrigin).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            }
            else if (builder.Environment.IsDevelopment())
            {
                // Development: localhost varyantlarına izin ver
                policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:5100")
                      .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            }
            else
            {
                // Production'da AllowedOrigin zorunlu — yanlış config'e karşı fail-closed
                throw new InvalidOperationException(
                    "AllowedOrigin must be configured in production. Set the 'AllowedOrigin' environment variable.");
            }
        }))
        .AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("api", o =>
            {
                o.PermitLimit = 60;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit = 0;
            });
            // Geliştirme ortamında rate limiting etkisiz hâle getirilir
            if (builder.Environment.IsDevelopment())
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    _ => RateLimitPartition.GetNoLimiter("dev"));
        })
        .AddOpenApi();

    var app = builder.Build();

    // Development ortamında migration uygula ve seed verisini yükle
    if (app.Environment.IsDevelopment())
        await DatabaseSeeder.SeedAsync(app.Services);

    // MongoDB AuditLogs index'lerini oluştur (idempotent — varsa atlar)
    await app.EnsureAuditIndexes();

    // CORS — OPTIONS preflight'ın başka middleware'e takılmaması için en önce
    app.UseCors("FrontendPolicy");

    // SessionId scope korelasyonu
    app.UseMiddleware<SessionIdMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseRateLimiter();
    app.MapControllers().RequireRateLimiting("api");
    app.MapHubs();    // Infrastructure/Realtime/HubEndpointExtensions
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));

    await app.RunAsync();
}
catch (Exception ex)
{
    logger.Error(ex, "Uygulama başlatılamadı.");
    throw;
}
finally
{
    LogManager.Shutdown();
}

// WebApplicationFactory<Program> için gereklidir
public partial class Program { }
