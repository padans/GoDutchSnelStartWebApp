-- Voegt AdministrationName toe aan dbo.TenantSnelStartConnections: de vrije naam van de
-- SnelStart-administratie waar de maatwerksleutel bij hoort. De SnelStart B2B v2-API geeft
-- deze naam niet terug, dus hij wordt bij het invoeren van de sleutel vastgelegd.
--
-- Eén administratie per tenant; dit is een enkel tekstveld, geen aparte tabel.

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF COL_LENGTH('dbo.TenantSnelStartConnections', 'AdministrationName') IS NULL
    ALTER TABLE dbo.TenantSnelStartConnections ADD AdministrationName NVARCHAR(200) NULL;
GO

-- ─── TenantSnelStartConnections_GetByTenantId ───────────────────────────────
ALTER PROCEDURE dbo.TenantSnelStartConnections_GetByTenantId
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,TenantId,ConnectionType,AuthUrl,ApiBaseUrl,AdministrationName,
           SubscriptionKeyEncrypted,ClientKeyEncrypted,
           OAuthAccessTokenEncrypted,OAuthRefreshTokenEncrypted,OAuthExpiresUtc,
           IsActive,CreatedUtc,ModifiedUtc,KeyExpiresUtc,ExpiryWarningSentUtc
    FROM dbo.TenantSnelStartConnections WHERE TenantId=@TenantId;
END
GO

-- ─── TenantSnelStartConnections_GetById ─────────────────────────────────────
ALTER PROCEDURE dbo.TenantSnelStartConnections_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,TenantId,ConnectionType,AuthUrl,ApiBaseUrl,AdministrationName,
           SubscriptionKeyEncrypted,ClientKeyEncrypted,
           OAuthAccessTokenEncrypted,OAuthRefreshTokenEncrypted,OAuthExpiresUtc,
           IsActive,CreatedUtc,ModifiedUtc,KeyExpiresUtc,ExpiryWarningSentUtc
    FROM dbo.TenantSnelStartConnections WHERE Id=@Id;
END
GO

-- ─── TenantSnelStartConnections_GetExpiringCustomKey ────────────────────────
ALTER PROCEDURE dbo.TenantSnelStartConnections_GetExpiringCustomKey
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,TenantId,ConnectionType,AuthUrl,ApiBaseUrl,AdministrationName,
           SubscriptionKeyEncrypted,ClientKeyEncrypted,
           OAuthAccessTokenEncrypted,OAuthRefreshTokenEncrypted,OAuthExpiresUtc,
           IsActive,CreatedUtc,ModifiedUtc,KeyExpiresUtc,ExpiryWarningSentUtc
    FROM dbo.TenantSnelStartConnections
    WHERE ConnectionType='CustomKey'
      AND IsActive=1
      AND KeyExpiresUtc IS NOT NULL
      AND KeyExpiresUtc <= DATEADD(DAY,7,GETUTCDATE())
      AND ExpiryWarningSentUtc IS NULL;
END
GO

-- ─── TenantSnelStartConnections_Insert ─────────────────────────────────────
ALTER PROCEDURE dbo.TenantSnelStartConnections_Insert
    @Id                         UNIQUEIDENTIFIER,
    @TenantId                   UNIQUEIDENTIFIER,
    @ConnectionType             NVARCHAR(50),
    @AuthUrl                    NVARCHAR(500),
    @ApiBaseUrl                 NVARCHAR(500),
    @AdministrationName         NVARCHAR(200) = NULL,
    @SubscriptionKeyEncrypted   NVARCHAR(MAX),
    @ClientKeyEncrypted         NVARCHAR(MAX),
    @OAuthAccessTokenEncrypted  NVARCHAR(MAX),
    @OAuthRefreshTokenEncrypted NVARCHAR(MAX),
    @OAuthExpiresUtc            DATETIME2,
    @IsActive                   BIT,
    @CreatedUtc                 DATETIME2,
    @ModifiedUtc                DATETIME2,
    @KeyExpiresUtc              DATETIME2,
    @ExpiryWarningSentUtc       DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.TenantSnelStartConnections(
        Id,TenantId,ConnectionType,AuthUrl,ApiBaseUrl,AdministrationName,
        SubscriptionKeyEncrypted,ClientKeyEncrypted,
        OAuthAccessTokenEncrypted,OAuthRefreshTokenEncrypted,OAuthExpiresUtc,
        IsActive,CreatedUtc,ModifiedUtc,KeyExpiresUtc,ExpiryWarningSentUtc)
    VALUES(
        @Id,@TenantId,@ConnectionType,@AuthUrl,@ApiBaseUrl,@AdministrationName,
        @SubscriptionKeyEncrypted,@ClientKeyEncrypted,
        @OAuthAccessTokenEncrypted,@OAuthRefreshTokenEncrypted,@OAuthExpiresUtc,
        @IsActive,@CreatedUtc,@ModifiedUtc,@KeyExpiresUtc,@ExpiryWarningSentUtc);
END
GO

-- ─── TenantSnelStartConnections_Update ─────────────────────────────────────
ALTER PROCEDURE dbo.TenantSnelStartConnections_Update
    @Id                         UNIQUEIDENTIFIER,
    @TenantId                   UNIQUEIDENTIFIER,
    @ConnectionType             NVARCHAR(50),
    @AuthUrl                    NVARCHAR(500),
    @ApiBaseUrl                 NVARCHAR(500),
    @AdministrationName         NVARCHAR(200) = NULL,
    @SubscriptionKeyEncrypted   NVARCHAR(MAX),
    @ClientKeyEncrypted         NVARCHAR(MAX),
    @OAuthAccessTokenEncrypted  NVARCHAR(MAX),
    @OAuthRefreshTokenEncrypted NVARCHAR(MAX),
    @OAuthExpiresUtc            DATETIME2,
    @IsActive                   BIT,
    @ModifiedUtc                DATETIME2,
    @KeyExpiresUtc              DATETIME2,
    @ExpiryWarningSentUtc       DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.TenantSnelStartConnections SET
        TenantId=@TenantId,ConnectionType=@ConnectionType,
        AuthUrl=@AuthUrl,ApiBaseUrl=@ApiBaseUrl,
        AdministrationName=@AdministrationName,
        SubscriptionKeyEncrypted=@SubscriptionKeyEncrypted,
        ClientKeyEncrypted=@ClientKeyEncrypted,
        OAuthAccessTokenEncrypted=@OAuthAccessTokenEncrypted,
        OAuthRefreshTokenEncrypted=@OAuthRefreshTokenEncrypted,
        OAuthExpiresUtc=@OAuthExpiresUtc,IsActive=@IsActive,
        ModifiedUtc=@ModifiedUtc,KeyExpiresUtc=@KeyExpiresUtc,
        ExpiryWarningSentUtc=@ExpiryWarningSentUtc
    WHERE Id=@Id;
END
GO
