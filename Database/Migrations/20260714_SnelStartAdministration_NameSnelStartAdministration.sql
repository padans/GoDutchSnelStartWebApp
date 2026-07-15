-- SnelStart administratienaam als apart veld naast de bestaande Name
ALTER TABLE dbo.SnelStartAdministrations
    ADD NameSnelStartAdministration NVARCHAR(200) NULL;
GO

-- GetById
CREATE OR ALTER PROCEDURE dbo.SnelStartAdministrations_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, TenantId, Name, NameSnelStartAdministration,
           AdministrationClientKeyEncrypted, IsActive, CreatedUtc, ModifiedUtc
    FROM dbo.SnelStartAdministrations
    WHERE Id = @Id;
END
GO

-- GetByTenantId
CREATE OR ALTER PROCEDURE dbo.SnelStartAdministrations_GetByTenantId
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, TenantId, Name, NameSnelStartAdministration,
           AdministrationClientKeyEncrypted, IsActive, CreatedUtc, ModifiedUtc
    FROM dbo.SnelStartAdministrations
    WHERE TenantId = @TenantId
    ORDER BY Name;
END
GO

-- Insert
CREATE OR ALTER PROCEDURE dbo.SnelStartAdministrations_Insert
    @Id                             UNIQUEIDENTIFIER,
    @TenantId                       UNIQUEIDENTIFIER,
    @Name                           NVARCHAR(200),
    @NameSnelStartAdministration    NVARCHAR(200) = NULL,
    @AdministrationClientKeyEncrypted NVARCHAR(MAX),
    @IsActive                       BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.SnelStartAdministrations
        (Id, TenantId, Name, NameSnelStartAdministration, AdministrationClientKeyEncrypted, IsActive, CreatedUtc)
    VALUES
        (@Id, @TenantId, @Name, @NameSnelStartAdministration, @AdministrationClientKeyEncrypted, @IsActive, GETUTCDATE());
END
GO

-- Update
CREATE OR ALTER PROCEDURE dbo.SnelStartAdministrations_Update
    @Id                             UNIQUEIDENTIFIER,
    @TenantId                       UNIQUEIDENTIFIER,
    @Name                           NVARCHAR(200),
    @NameSnelStartAdministration    NVARCHAR(200) = NULL,
    @AdministrationClientKeyEncrypted NVARCHAR(MAX),
    @IsActive                       BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.SnelStartAdministrations
    SET Name                          = @Name,
        NameSnelStartAdministration   = @NameSnelStartAdministration,
        AdministrationClientKeyEncrypted = @AdministrationClientKeyEncrypted,
        IsActive                      = @IsActive,
        ModifiedUtc                   = GETUTCDATE()
    WHERE Id = @Id AND TenantId = @TenantId;
END
GO
