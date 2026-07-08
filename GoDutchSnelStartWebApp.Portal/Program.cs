using GoDutchSnelStartWebApp.Portal.Api.Interfaces;
using GoDutchSnelStartWebApp.Portal.Components;
using GoDutchSnelStartWebApp.Portal.Configuration;
using GoDutchSnelStartWebApp.Portal.Api.Services;
using GoDutchSnelStartWebApp.Portal.Services;


namespace GoDutchSnelStartWebApp.Portal
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddHttpClient<IBackendApiClient, BackendApiClient>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:5275/");
            });
            builder.Services.Configure<PortalTenantOptions>(
                    builder.Configuration.GetSection(PortalTenantOptions.SectionName));
            builder.Services.AddScoped<AppSession>();

            builder.Services.AddHttpClient<IGoDutchBackendApiClient, GoDutchBackendApiClient>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:5275/");
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
               .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
