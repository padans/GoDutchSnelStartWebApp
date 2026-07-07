using GoDutchSnelStartWebApp.Application.BankAccounts.Interfaces;
using GoDutchSnelStartWebApp.Application.BankAccounts.Services;
using GoDutchSnelStartWebApp.Application.GoDutchLeads.Interfaces;
using GoDutchSnelStartWebApp.Application.GoDutchLeads.Services;
using GoDutchSnelStartWebApp.Application.BankAccountSettings.Interfaces;
using GoDutchSnelStartWebApp.Application.BankAccountSettings.Services;
using GoDutchSnelStartWebApp.Application.ConnectivityTests.Interfaces;
using GoDutchSnelStartWebApp.Application.ConnectivityTests.Services;
using GoDutchSnelStartWebApp.Application.GoDutchConnections.Interfaces;
using GoDutchSnelStartWebApp.Application.GoDutchConnections.Services;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Services;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using GoDutchSnelStartWebApp.Application.MyPos.Services;
using GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Interfaces;
using GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Services;
using GoDutchSnelStartWebApp.Application.SnelStartConnections.Interfaces;
using GoDutchSnelStartWebApp.Application.SnelStartConnections.Services;
using GoDutchSnelStartWebApp.Application.Tenants.Interfaces;
using GoDutchSnelStartWebApp.Application.Tenants.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GoDutchSnelStartWebApp.Application.DependencyInjection;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IBankAccountService, BankAccountService>();
        services.AddScoped<IBankAccountSettingsService, BankAccountSettingsService>();
        services.AddScoped<IConnectionTestService, ConnectionTestService>();

        services.AddScoped<IBalanceCalculationService, BalanceCalculationService>();

        services.AddScoped<IGoDutchMt940ExportService, GoDutchMt940ExportService>();
        services.AddScoped<IGoDutchCamt053ExportService, GoDutchCamt053ExportService>();
        services.AddScoped<IGoDutchSnelStartExportService, GoDutchSnelStartExportService>();
        services.AddScoped<IGoDutchSnelStartImportService, GoDutchSnelStartImportService>();
        services.AddScoped<IGoDutchSnelStartAutoImportService, GoDutchSnelStartAutoImportService>();
        services.AddScoped<IGoDutchWebhookImportService, GoDutchWebhookImportService>();
        services.AddScoped<IGoDutchAutoSyncService, GoDutchAutoSyncService>();

        services.AddScoped<ITenantGoDutchConnectionService, TenantGoDutchConnectionService>();
        services.AddScoped<ISnelStartAdministrationService, SnelStartAdministrationService>();
        services.AddScoped<IBankAccountSnelStartLinkService, BankAccountSnelStartLinkService>();
        services.AddScoped<ITenantSnelStartConnectionService, TenantSnelStartConnectionService>();

        services.AddScoped<ITenantMyPosConnectionService, TenantMyPosConnectionService>();
        services.AddScoped<IMyPosTransactionTypeMappingService, MyPosTransactionTypeMappingService>();
        services.AddScoped<IMyPosTransactionTotalService, MyPosTransactionTotalService>();
        services.AddScoped<IMyPosExportBatchService, MyPosExportBatchService>();
        services.AddScoped<IMyPosAutoSyncService, MyPosAutoSyncService>();

        services.AddScoped<IBankAccountResyncService, BankAccountResyncService>();
        services.AddScoped<IGoDutchLeadService, GoDutchLeadService>();

        return services;
    }
}
