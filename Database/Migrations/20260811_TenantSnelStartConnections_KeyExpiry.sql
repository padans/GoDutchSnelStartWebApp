-- Voeg KeyExpiresUtc en ExpiryWarningSentUtc toe aan TenantSnelStartConnections
-- Maatwerksleutel is 90 dagen geldig; 7 dagen van tevoren een waarschuwingsmail

ALTER TABLE dbo.TenantSnelStartConnections ADD
    KeyExpiresUtc        DATETIME2 NULL,
    ExpiryWarningSentUtc DATETIME2 NULL;
GO

-- Bestaande CustomKey-koppelingen: vervaldatum = aanmaakdatum + 90 dagen
UPDATE dbo.TenantSnelStartConnections
SET KeyExpiresUtc = DATEADD(DAY, 90, CreatedUtc)
WHERE ConnectionType = 'CustomKey';
GO

-- ─── TenantSnelStartConnections_GetByTenantId ────────────────────────────────
ALTER PROCEDURE dbo.TenantSnelStartConnections_GetByTenantId
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, TenantId, ConnectionType, AuthUrl, ApiBaseUrl,
           SubscriptionKeyEncrypted, ClientKeyEncrypted,
           OAuthAccessTokenEncrypted, OAuthRefreshTokenEncrypted, OAuthExpiresUtc,
           IsActive, CreatedUtc, ModifiedUtc,
           KeyExpiresUtc, ExpiryWarningSentUtc
    FROM dbo.TenantSnelStartConnections
    WHERE TenantId = @TenantId;
END
GO

-- ─── TenantSnelStartConnections_GetById ──────────────────────────────────────
ALTER PROCEDURE dbo.TenantSnelStartConnections_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, TenantId, ConnectionType, AuthUrl, ApiBaseUrl,
           SubscriptionKeyEncrypted, ClientKeyEncrypted,
           OAuthAccessTokenEncrypted, OAuthRefreshTokenEncrypted, OAuthExpiresUtc,
           IsActive, CreatedUtc, ModifiedUtc,
           KeyExpiresUtc, ExpiryWarningSentUtc
    FROM dbo.TenantSnelStartConnections
    WHERE Id = @Id;
END
GO

-- ─── TenantSnelStartConnections_Insert ───────────────────────────────────────
ALTER PROCEDURE dbo.TenantSnelStartConnections_Insert
    @Id                        UNIQUEIDENTIFIER,
    @TenantId                  UNIQUEIDENTIFIER,
    @ConnectionType            NVARCHAR(50),
    @AuthUrl                   NVARCHAR(500),
    @ApiBaseUrl                NVARCHAR(500),
    @SubscriptionKeyEncrypted  NVARCHAR(MAX),
    @ClientKeyEncrypted        NVARCHAR(MAX),
    @OAuthAccessTokenEncrypted NVARCHAR(MAX),
    @OAuthRefreshTokenEncrypted NVARCHAR(MAX),
    @OAuthExpiresUtc           DATETIME2,
    @IsActive                  BIT,
    @CreatedUtc                DATETIME2,
    @ModifiedUtc               DATETIME2,
    @KeyExpiresUtc             DATETIME2,
    @ExpiryWarningSentUtc      DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.TenantSnelStartConnections
        (Id, TenantId, ConnectionType, AuthUrl, ApiBaseUrl,
         SubscriptionKeyEncrypted, ClientKeyEncrypted,
         OAuthAccessTokenEncrypted, OAuthRefreshTokenEncrypted, OAuthExpiresUtc,
         IsActive, CreatedUtc, ModifiedUtc,
         KeyExpiresUtc, ExpiryWarningSentUtc)
    VALUES
        (@Id, @TenantId, @ConnectionType, @AuthUrl, @ApiBaseUrl,
         @SubscriptionKeyEncrypted, @ClientKeyEncrypted,
         @OAuthAccessTokenEncrypted, @OAuthRefreshTokenEncrypted, @OAuthExpiresUtc,
         @IsActive, @CreatedUtc, @ModifiedUtc,
         @KeyExpiresUtc, @ExpiryWarningSentUtc);
END
GO

-- ─── TenantSnelStartConnections_Update ───────────────────────────────────────
ALTER PROCEDURE dbo.TenantSnelStartConnections_Update
    @Id                        UNIQUEIDENTIFIER,
    @TenantId                  UNIQUEIDENTIFIER,
    @ConnectionType            NVARCHAR(50),
    @AuthUrl                   NVARCHAR(500),
    @ApiBaseUrl                NVARCHAR(500),
    @SubscriptionKeyEncrypted  NVARCHAR(MAX),
    @ClientKeyEncrypted        NVARCHAR(MAX),
    @OAuthAccessTokenEncrypted NVARCHAR(MAX),
    @OAuthRefreshTokenEncrypted NVARCHAR(MAX),
    @OAuthExpiresUtc           DATETIME2,
    @IsActive                  BIT,
    @ModifiedUtc               DATETIME2,
    @KeyExpiresUtc             DATETIME2,
    @ExpiryWarningSentUtc      DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.TenantSnelStartConnections SET
        TenantId                  = @TenantId,
        ConnectionType            = @ConnectionType,
        AuthUrl                   = @AuthUrl,
        ApiBaseUrl                = @ApiBaseUrl,
        SubscriptionKeyEncrypted  = @SubscriptionKeyEncrypted,
        ClientKeyEncrypted        = @ClientKeyEncrypted,
        OAuthAccessTokenEncrypted = @OAuthAccessTokenEncrypted,
        OAuthRefreshTokenEncrypted = @OAuthRefreshTokenEncrypted,
        OAuthExpiresUtc           = @OAuthExpiresUtc,
        IsActive                  = @IsActive,
        ModifiedUtc               = @ModifiedUtc,
        KeyExpiresUtc             = @KeyExpiresUtc,
        ExpiryWarningSentUtc      = @ExpiryWarningSentUtc
    WHERE Id = @Id;
END
GO

-- ─── TenantSnelStartConnections_GetExpiringCustomKey (nieuw) ─────────────────
-- Geeft actieve CustomKey-koppelingen die binnen 7 dagen verlopen
-- én waarvoor nog geen waarschuwingsmail is verstuurd
CREATE PROCEDURE dbo.TenantSnelStartConnections_GetExpiringCustomKey
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, TenantId, ConnectionType, AuthUrl, ApiBaseUrl,
           SubscriptionKeyEncrypted, ClientKeyEncrypted,
           OAuthAccessTokenEncrypted, OAuthRefreshTokenEncrypted, OAuthExpiresUtc,
           IsActive, CreatedUtc, ModifiedUtc,
           KeyExpiresUtc, ExpiryWarningSentUtc
    FROM dbo.TenantSnelStartConnections
    WHERE ConnectionType = 'CustomKey'
      AND IsActive = 1
      AND KeyExpiresUtc IS NOT NULL
      AND KeyExpiresUtc <= DATEADD(DAY, 7, GETUTCDATE())
      AND ExpiryWarningSentUtc IS NULL;
END
GO

-- ─── TenantSnelStartConnections_MarkExpirySent (nieuw) ───────────────────────
CREATE PROCEDURE dbo.TenantSnelStartConnections_MarkExpirySent
    @Id                   UNIQUEIDENTIFIER,
    @ExpiryWarningSentUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.TenantSnelStartConnections
    SET ExpiryWarningSentUtc = @ExpiryWarningSentUtc,
        ModifiedUtc          = @ExpiryWarningSentUtc
    WHERE Id = @Id;
END
GO
