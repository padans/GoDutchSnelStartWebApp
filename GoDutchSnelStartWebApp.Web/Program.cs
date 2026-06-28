using System.Runtime.Versioning;
using GoDutchSnelStartWebApp.Application.DependencyInjection;
using GoDutchSnelStartWebApp.Infrastructure.DependencyInjection;
using GoDutchSnelStartWebApp.Web.Middleware;
using Serilog;

namespace GoDutchSnelStartWebApp.Web;

public class Program
{
    [SupportedOSPlatform("windows")]
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(AppContext.BaseDirectory, "logs", "bootstrap-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true)
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting GoDutchSnelStartWebApp");

            var builder = WebApplication.CreateBuilder(args);
            Log.Information(
    "GoDutchAutoSync.Enabled from configuration = {Enabled}, IntervalSeconds = {IntervalSeconds}",
    builder.Configuration.GetValue<bool>("GoDutchAutoSync:Enabled"),
    builder.Configuration.GetValue<int?>("GoDutchAutoSync:IntervalSeconds"));

            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
                    .MinimumLevel.Override("GoDutchSnelStartWebApp", Serilog.Events.LogEventLevel.Debug)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File(
                        Path.Combine(AppContext.BaseDirectory, "logs", "app-.txt"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 14,
                        shared: true);
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructureServices(builder.Configuration);
            builder.Services.AddHealthChecks();

            var app = builder.Build();

            Log.Information("Serilog file test after app build");

            app.UseSerilogRequestLogging();
            app.UseApiExceptionMiddleware();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.MapControllers();
            app.MapHealthChecks("/health");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}