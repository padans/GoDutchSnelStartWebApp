using GoDutchSnelStartWebApp.Portal.Models;
using GoDutchSnelStartWebApp.Portal.Models.MyPos;

namespace GoDutchSnelStartWebApp.Portal.Api.Interfaces;

public interface IBackendApiClient
{
    Task<IReadOnlyList<BankAccountViewModel>> GetBankAccountsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<BankAccountViewModel?> GetBankAccountByIdAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default);

    Task<BankAccountSyncStatusViewModel?> GetSyncStatusAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default);

    Task<BankAccountResyncResultViewModel> ForceResyncAsync(
        Guid tenantId,
        Guid bankAccountId,
        DateTime fromUtc,
        CancellationToken cancellationToken = default);

    Task UpdateBankAccountSnelStartSettingsAsync(
        Guid tenantId,
        Guid bankAccountId,
        UpdateBankAccountSnelStartSettingsRequestViewModel request,
        CancellationToken cancellationToken = default);

    Task<BankAccountSettingsViewModel?> GetBankAccountSettingsAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default);

    Task SaveBankAccountSettingsAsync(
        Guid tenantId,
        Guid bankAccountId,
        BankAccountSettingsViewModel request,
        CancellationToken cancellationToken = default);

    Task<ConnectionTestResultViewModel> TestSnelStartAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SnelStartDagboekLookupViewModel>> GetSnelStartDagboekenAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SnelStartGrootboekLookupViewModel>> GetSnelStartGrootboekenAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateBankAccountAsync(
        Guid tenantId,
        CreateBankAccountRequestViewModel request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SnelStartAdministrationViewModel>> GetSnelStartAdministrationsByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateSnelStartAdministrationAsync(
        CreateSnelStartAdministrationRequestViewModel request,
        CancellationToken cancellationToken = default);

    Task UpdateSnelStartAdministrationAsync(
        Guid id,
        UpdateSnelStartAdministrationRequestViewModel request,
        CancellationToken cancellationToken = default);

    Task<BankAccountSnelStartLinkViewModel?> GetBankAccountSnelStartLinkByBankAccountIdAsync(
        Guid bankAccountId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateBankAccountSnelStartLinkAsync(
        CreateBankAccountSnelStartLinkRequestViewModel request,
        CancellationToken cancellationToken = default);

    Task UpdateBankAccountSnelStartLinkAsync(
        Guid id,
        UpdateBankAccountSnelStartLinkRequestViewModel request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SnelStartGrootboekLookupViewModel>> GetTenantSnelStartGrootboekenAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<SnelStartGrootboekLookupViewModel> CreateTenantSnelStartGrootboekAsync(
        Guid tenantId,
        CreateSnelStartGrootboekRequestViewModel request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MyPosTransactionTypeMappingViewModel>> GetMyPosTransactionTypeMappingsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<MyPosTransactionTypeMappingViewModel> UpsertMyPosTransactionTypeMappingAsync(
        Guid tenantId,
        UpsertMyPosTransactionTypeMappingRequestViewModel request,
        CancellationToken cancellationToken = default);


    Task<TenantMyPosConnectionViewModel?> GetTenantMyPosConnectionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<MyPosTransactionImportResultViewModel> ImportMyPosTransactionsAsync(
        Guid tenantId,
        Guid tenantMyPosConnectionId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MyPosRawTransactionViewModel>> GetMyPosRawTransactionsAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MyPosTransactionTotalViewModel>> GetMyPosTransactionTotalsAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        bool includeExported = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SnelStartBtwTariefLookupViewModel>> GetTenantSnelStartBtwTarievenAsync(
    Guid tenantId,
    CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SnelStartDagboekLookupViewModel>> GetTenantSnelStartDagboekenAsync(
    Guid tenantId,
    CancellationToken cancellationToken = default);

    Task UpdateTenantMyPosConnectionAsync(
        Guid tenantId,
        Guid connectionId,
        UpdateTenantMyPosConnectionRequestViewModel request,
        CancellationToken cancellationToken = default);

    Task<MyPosExportBatchViewModel> CreateMyPosExportBatchConceptAsync(
    Guid tenantId,
    CreateMyPosExportBatchRequestViewModel request,
    CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MyPosExportBatchViewModel>> GetMyPosExportBatchesAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<MyPosExportBatchExportResultViewModel> ExportMyPosBatchToSnelStartAsync(
    Guid tenantId,
    Guid batchId,
    CancellationToken cancellationToken = default);

    Task<MyPosAutoSyncSettingsViewModel> GetMyPosAutoSyncSettingsAsync(
        CancellationToken cancellationToken = default);

    Task UpdateMyPosAutoSyncSettingsAsync(
        MyPosAutoSyncSettingsViewModel settings,
        CancellationToken cancellationToken = default);

    Task TriggerMyPosAutoSyncAsync(
        CancellationToken cancellationToken = default);

    Task<Guid> OnboardTenantAsync(
        OnboardTenantRequestViewModel request,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadNotificationCountAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationViewModel>> GetUnreadNotificationsAsync(
        CancellationToken cancellationToken = default);

    Task MarkNotificationAsReadAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task MarkAllNotificationsAsReadAsync(
        CancellationToken cancellationToken = default);

    Task<MyPosTransactionTypeStatusResultViewModel> GetMyPosTransactionTypeStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task SubmitGoDutchLeadAsync(
        GoDutchLeadViewModel lead,
        CancellationToken cancellationToken = default);

    Task<AppUserViewModel?> LoginAsync(
        LoginRequestViewModel request,
        CancellationToken cancellationToken = default);
}
