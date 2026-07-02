using NLog;
using NLog.Web;
using Scalar.AspNetCore;
using StudentManagement.Agent;
using StudentManagement.Agent.Extensions;
using StudentManagement.Agent.Middleware;

var logger = LogManager.Setup()
    .LoadConfigurationFromFile("nlog.config")
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddOpenApi();
    builder.Services.AddAgentServices(builder.Configuration);
    builder.Services.AddControllers();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseHttpsRedirection();
    app.MapControllers();
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Agent uygulaması başlatılamadı.");
    throw;
}
finally
{
    LogManager.Shutdown();
}

