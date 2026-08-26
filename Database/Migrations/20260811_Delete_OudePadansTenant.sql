-- Verwijder oude Padans B.V. tenant (6f082b3d) en alle gekoppelde records
-- Status: Cancelled, IsActive=False, geen transacties of import-runs
-- Uitgevoerd: 2026-08-11

DECLARE @tid UNIQUEIDENTIFIER = '6F082B3D-293B-48A6-AE4B-F42FADB4439D';

BEGIN TRANSACTION;

BEGIN TRY

    -- 1. BankAccountSnelStartLinks (referentie naar BankAccounts EN SnelStartAdministrations)
    DELETE dbo.BankAccountSnelStartLinks
    WHERE BankAccountId IN (SELECT Id FROM dbo.BankAccounts WHERE TenantId = @tid);

    -- 2. BankAccountSettings (referentie naar BankAccounts)
    DELETE dbo.BankAccountSettings
    WHERE BankAccountId IN (SELECT Id FROM dbo.BankAccounts WHERE TenantId = @tid);

    -- 3. GoDutchImportRuns (referentie naar BankAccounts, 0 rijen maar voor zekerheid)
    DELETE dbo.GoDutchImportRuns
    WHERE BankAccountId IN (SELECT Id FROM dbo.BankAccounts WHERE TenantId = @tid);

    -- 4. BankAccounts
    DELETE dbo.BankAccounts WHERE TenantId = @tid;

    -- 5. SnelStartAdministrations
    DELETE dbo.SnelStartAdministrations WHERE TenantId = @tid;

    -- 6. MyPosTransactionTypeMappings (32 rijen)
    DELETE dbo.MyPosTransactionTypeMappings WHERE TenantId = @tid;

    -- 7. MyPosRawTransactions (0 rijen, voor zekerheid)
    DELETE dbo.MyPosRawTransactions
    WHERE TenantMyPosConnectionId IN (SELECT Id FROM dbo.TenantMyPosConnections WHERE TenantId = @tid);

    -- 8. MyPosExportBatches (0 rijen, voor zekerheid)
    DELETE dbo.MyPosExportBatches
    WHERE TenantMyPosConnectionId IN (SELECT Id FROM dbo.TenantMyPosConnections WHERE TenantId = @tid);

    -- 9. TenantMyPosConnections
    DELETE dbo.TenantMyPosConnections WHERE TenantId = @tid;

    -- 10. TenantGoDutchConnections
    DELETE dbo.TenantGoDutchConnections WHERE TenantId = @tid;

    -- 11. TenantSnelStartConnections
    DELETE dbo.TenantSnelStartConnections WHERE TenantId = @tid;

    -- 12. Tenant zelf
    DELETE dbo.Tenants WHERE Id = @tid;

    COMMIT TRANSACTION;
    PRINT 'Tenant en alle gekoppelde records succesvol verwijderd.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'FOUT — rollback uitgevoerd:';
    PRINT ERROR_MESSAGE();
END CATCH;
