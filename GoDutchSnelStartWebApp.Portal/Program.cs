using GoDutchSnelStartWebApp.Portal.Api.Interfaces;
using GoDutchSnelStartWebApp.Portal.Components;
using GoDutchSnelStartWebApp.Portal.Configuration;
using GoDutchSnelStartWebApp.Portal.Api.Services;
using GoDutchSnelStartWebApp.Portal.Services;
using Serilog;
using Serilog.Events;


namespace GoDutchSnelStartWebApp.Portal
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "portal-.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    path: logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            var apiBaseUrl = builder.Configuration["BackendApi:BaseUrl"] ?? "http://localhost:5275/";

            builder.Services.AddHttpClient<IBackendApiClient, BackendApiClient>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            });
            builder.Services.Configure<PortalTenantOptions>(
                    builder.Configuration.GetSection(PortalTenantOptions.SectionName));
            builder.Services.AddScoped<AppSession>();

            builder.Services.AddHttpClient<IGoDutchBackendApiClient, GoDutchBackendApiClient>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
               .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
