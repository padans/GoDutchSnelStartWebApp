-- Referentietabel voor bekende myPOS transactietypes (globaal, niet per tenant)
CREATE TABLE dbo.MyPosTransactionTypeDefinitions (
    Code        NVARCHAR(10)  NOT NULL CONSTRAINT PK_MyPosTransactionTypeDefinitions PRIMARY KEY,
    Description NVARCHAR(200) NOT NULL,
    CreatedUtc  DATETIME2     NOT NULL CONSTRAINT DF_MyPosTransactionTypeDefinitions_CreatedUtc DEFAULT GETUTCDATE()
);
GO

INSERT INTO dbo.MyPosTransactionTypeDefinitions (Code, Description) VALUES
('001', 'Fee'),
('002', 'Cash Withdrawal'),
('003', 'Outgoing Bank Transfer'),
('004', 'Balance Transfer'),
('005', 'E-money Redemption'),
('006', 'Account Funding'),
('007', 'Original Credit'),
('008', 'POS Purchase'),
('009', 'Online Purchase'),
('010', 'Internal Transfer'),
('011', 'Refund'),
('012', 'Money Request'),
('013', 'Payment'),
('014', 'Direct Debit'),
('015', 'Pre-Authorization'),
('016', 'MOTO Payment'),
('017', 'MOTO Refund'),
('018', 'MOTO Pre-Authorization'),
('019', 'ATM Deposit'),
('022', 'NFC Payment'),
('023', 'ATM Surcharge'),
('024', 'Withdrawal'),
('026', 'Utility Bills'),
('035', 'Payment Request'),
('036', 'Payment Return'),
('037', 'Reverse With Hold'),
('038', 'Reverse Release'),
('039', 'Commission'),
('040', 'Cash Funding'),
('041', 'Negative Set Off'),
('042', 'Charge Back'),
('501', 'Payment on POS');
GO

-- Stored procedure voor seed: voegt ontbrekende types toe aan een tenant
CREATE OR ALTER PROCEDURE dbo.MyPosTransactionTypeMappings_SeedFromDefinitions
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.MyPosTransactionTypeMappings
        (Id, TenantId, TransactionCode, Description, BtwBerekening, BtwSoort, BtwPercentage, IsActive, CreatedUtc)
    SELECT
        NEWID(),
        @TenantId,
        d.Code,
        d.Description,
        'Geen',
        'Geen',
        0,
        1,
        GETUTCDATE()
    FROM dbo.MyPosTransactionTypeDefinitions d
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.MyPosTransactionTypeMappings m
        WHERE m.TenantId = @TenantId
          AND m.TransactionCode = d.Code
    );

    SELECT @@ROWCOUNT AS Seeded;
END;
GO
