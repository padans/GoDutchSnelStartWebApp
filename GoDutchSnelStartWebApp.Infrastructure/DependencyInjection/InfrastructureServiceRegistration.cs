using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;
using GoDutchSnelStartWebApp.Infrastructure.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.SnelStart;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.ConnectivityTests.Interfaces;
using GoDutchSnelStartWebApp.Application.GoDutchAccounts.Interfaces;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using GoDutchSnelStartWebApp.Application.Notifications.Interfaces;
using GoDutchSnelStartWebApp.Infrastructure.ExternalServices.Email;
using GoDutchSnelStartWebApp.Application.SnelStartLookups.Interfaces;
using GoDutchSnelStartWebApp.Infrastructure.ExternalServices;
using GoDutchSnelStartWebApp.Infrastructure.ExternalServices.BackgroundWorkers;
using GoDutchSnelStartWebApp.Infrastructure.ExternalServices.Generators;
using GoDutchSnelStartWebApp.Infrastructure.ExternalServices.GoDutch;
using GoDutchSnelStartWebApp.Infrastructure.ExternalServices.MyPos;
using GoDutchSnelStartWebApp.Infrastructure.ExternalServices.SnelStart;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using GoDutchSnelStartWebApp.Infrastructure.Repositories.MyPos;
using GoDutchSnelStartWebApp.Infrastructure.Repositories.SnelStart;
using GoDutchSnelStartWebApp.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.Versioning;

namespace GoDutchSnelStartWebApp.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddSingleton<ISecretEncryptionService, DpapiSecretEncryptionService>();

        services.Configure<GoDutchAutoSyncOptions>(
            configuration.GetSection(GoDutchAutoSyncOptions.SectionName));
        services.Configure<MyPosAutoSyncOptions>(
            configuration.GetSection(MyPosAutoSyncOptions.SectionName));
        services.Configure<SnelStartImportRetryOptions>(
            configuration.GetSection(SnelStartImportRetryOptions.SectionName));
        services.Configure<GoDutchApiRetryOptions>(
            configuration.GetSection(GoDutchApiRetryOptions.SectionName));
        services.Configure<EmailOptions>(
            configuration.GetSection(EmailOptions.SectionName));

        // Repositories
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IBankAccountRepository, BankAccountRepository>();
        services.AddScoped<IBankAccountSettingsRepository, BankAccountSettingsRepository>();
        services.AddScoped<ITenantGoDutchConnectionRepository, TenantGoDutchConnectionRepository>();
        services.AddScoped<ISnelStartAdministrationRepository, SnelStartAdministrationRepository>();
        services.AddScoped<ITenantSnelStartConnectionRepository, TenantSnelStartConnectionRepository>();
        services.AddScoped<ITenantMyPosConnectionRepository, TenantMyPosConnectionRepository>();
        services.AddScoped<IMyPosTransactionTypeMappingRepository, MyPosTransactionTypeMappingRepository>();
        services.AddScoped<IBankAccountSnelStartLinkRepository, BankAccountSnelStartLinkRepository>();
        services.AddScoped<IGoDutchImportRunRepository, GoDutchImportRunRepository>();
        services.AddScoped<IMyPosExportBatchRepository, MyPosExportBatchRepository>();
        services.AddScoped<IMyPosRawTransactionRepository, MyPosRawTransactionRepository>();

        // HTTP clients (external services)
        services.AddHttpClient<IGoDutchTransactionService, GoDutchTransactionService>();
        services.AddHttpClient<IGoDutchAccountLookupService, GoDutchAccountLookupService>();
        services.AddHttpClient<ISnelStartConnectionTestClient, SnelStartConnectionTestClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHttpClient<ISnelStartLookupService, SnelStartLookupService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient<ISnelStartBankStatementImporter, SnelStartBankStatementImporter>();
        services.AddHttpClient<IMyPosTransactionImportService, MyPosTransactionImportService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddHttpClient<IMyPosExportBatchExportService, MyPosExportBatchExportService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        // Infrastructure generators
        services.AddScoped<IMt940Generator, Mt940Generator>();
        services.AddScoped<ICamt053Generator, Camt053Generator>();

        services.AddScoped<IGoDutchLeadRepository, GoDutchLeadRepository>();
        services.AddScoped<IMyPosAutoSyncSettingsRepository, MyPosAutoSyncSettingsRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IEmailNotificationService, SmtpEmailNotificationService>();

        // Background workers
        services.AddHostedService<GoDutchAutoSyncBackgroundWorker>();
        services.AddHostedService<MyPosAutoSyncBackgroundWorker>();

        return services;
    }
}
