-- Naam-kolom toevoegen aan TenantMyPosConnections
ALTER TABLE dbo.TenantMyPosConnections
    ADD Name NVARCHAR(200) NULL;
GO

-- GetByTenantId
CREATE OR ALTER PROCEDURE dbo.TenantMyPosConnections_GetByTenantId
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, TenantId, Name, AuthUrl, TransactionsApiBaseUrl, ClientId,
           ClientSecretEncrypted, ApiKeyEncrypted,
           SnelStartBankDagboekId, SnelStartBankDagboekNummer, SnelStartBankDagboekNaam,
           SnelStartBankIban, IsActive, CreatedUtc, ModifiedUtc
    FROM dbo.TenantMyPosConnections
    WHERE TenantId = @TenantId;
END
GO

-- GetById
CREATE OR ALTER PROCEDURE dbo.TenantMyPosConnections_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, TenantId, Name, AuthUrl, TransactionsApiBaseUrl, ClientId,
           ClientSecretEncrypted, ApiKeyEncrypted,
           SnelStartBankDagboekId, SnelStartBankDagboekNummer, SnelStartBankDagboekNaam,
           SnelStartBankIban, IsActive, CreatedUtc, ModifiedUtc
    FROM dbo.TenantMyPosConnections
    WHERE Id = @Id;
END
GO

-- GetAllActive
CREATE OR ALTER PROCEDURE dbo.TenantMyPosConnections_GetAllActive
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, TenantId, Name, AuthUrl, TransactionsApiBaseUrl, ClientId,
           ClientSecretEncrypted, ApiKeyEncrypted,
           SnelStartBankDagboekId, SnelStartBankDagboekNummer, SnelStartBankDagboekNaam,
           SnelStartBankIban, IsActive, CreatedUtc, ModifiedUtc
    FROM dbo.TenantMyPosConnections
    WHERE IsActive = 1;
END
GO

-- Insert
CREATE OR ALTER PROCEDURE dbo.TenantMyPosConnections_Insert
    @Id                          UNIQUEIDENTIFIER,
    @TenantId                    UNIQUEIDENTIFIER,
    @Name                        NVARCHAR(200),
    @AuthUrl                     NVARCHAR(500),
    @TransactionsApiBaseUrl      NVARCHAR(500),
    @ClientId                    NVARCHAR(256),
    @ClientSecretEncrypted       NVARCHAR(MAX)     = NULL,
    @ApiKeyEncrypted             NVARCHAR(MAX)     = NULL,
    @SnelStartBankDagboekId      UNIQUEIDENTIFIER  = NULL,
    @SnelStartBankDagboekNummer  NVARCHAR(50)      = NULL,
    @SnelStartBankDagboekNaam    NVARCHAR(500)     = NULL,
    @SnelStartBankIban           NVARCHAR(50)      = NULL,
    @IsActive                    BIT,
    @CreatedUtc                  DATETIME2,
    @ModifiedUtc                 DATETIME2         = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.TenantMyPosConnections (
        Id, TenantId, Name, AuthUrl, TransactionsApiBaseUrl, ClientId,
        ClientSecretEncrypted, ApiKeyEncrypted,
        SnelStartBankDagboekId, SnelStartBankDagboekNummer, SnelStartBankDagboekNaam,
        SnelStartBankIban, IsActive, CreatedUtc, ModifiedUtc
    )
    VALUES (
        @Id, @TenantId, @Name, @AuthUrl, @TransactionsApiBaseUrl, @ClientId,
        @ClientSecretEncrypted, @ApiKeyEncrypted,
        @SnelStartBankDagboekId, @SnelStartBankDagboekNummer, @SnelStartBankDagboekNaam,
        @SnelStartBankIban, @IsActive, @CreatedUtc, @ModifiedUtc
    );
END
GO

-- Update
CREATE OR ALTER PROCEDURE dbo.TenantMyPosConnections_Update
    @Id                          UNIQUEIDENTIFIER,
    @TenantId                    UNIQUEIDENTIFIER,
    @Name                        NVARCHAR(200),
    @AuthUrl                     NVARCHAR(500),
    @TransactionsApiBaseUrl      NVARCHAR(500),
    @ClientId                    NVARCHAR(256),
    @ClientSecretEncrypted       NVARCHAR(MAX)     = NULL,
    @ApiKeyEncrypted             NVARCHAR(MAX)     = NULL,
    @SnelStartBankDagboekId      UNIQUEIDENTIFIER  = NULL,
    @SnelStartBankDagboekNummer  NVARCHAR(50)      = NULL,
    @SnelStartBankDagboekNaam    NVARCHAR(500)     = NULL,
    @SnelStartBankIban           NVARCHAR(50)      = NULL,
    @IsActive                    BIT,
    @ModifiedUtc                 DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.TenantMyPosConnections
    SET Name                       = @Name,
        AuthUrl                    = @AuthUrl,
        TransactionsApiBaseUrl     = @TransactionsApiBaseUrl,
        ClientId                   = @ClientId,
        ClientSecretEncrypted      = @ClientSecretEncrypted,
        ApiKeyEncrypted            = @ApiKeyEncrypted,
        SnelStartBankDagboekId     = @SnelStartBankDagboekId,
        SnelStartBankDagboekNummer = @SnelStartBankDagboekNummer,
        SnelStartBankDagboekNaam   = @SnelStartBankDagboekNaam,
        SnelStartBankIban          = @SnelStartBankIban,
        IsActive                   = @IsActive,
        ModifiedUtc                = @ModifiedUtc
    WHERE Id = @Id AND TenantId = @TenantId;
END
GO
