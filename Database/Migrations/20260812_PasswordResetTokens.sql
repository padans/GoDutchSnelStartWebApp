-- Wachtwoord-resetfunctie: tabel + stored procedures
-- Token is 1 uur geldig, eenmalig te gebruiken

CREATE TABLE dbo.PasswordResetTokens (
    Id         UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    UserId     UNIQUEIDENTIFIER NOT NULL,
    Token      NVARCHAR(128)    NOT NULL,
    ExpiresUtc DATETIME2        NOT NULL,
    UsedUtc    DATETIME2        NULL,
    CreatedUtc DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_PasswordResetTokens_AppUsers
        FOREIGN KEY (UserId) REFERENCES dbo.AppUsers(Id)
);
GO
CREATE UNIQUE INDEX UX_PasswordResetTokens_Token ON dbo.PasswordResetTokens(Token);
GO

-- ─── PasswordResetTokens_Insert ──────────────────────────────────────────────
CREATE PROCEDURE dbo.PasswordResetTokens_Insert
    @Id         UNIQUEIDENTIFIER,
    @UserId     UNIQUEIDENTIFIER,
    @Token      NVARCHAR(128),
    @ExpiresUtc DATETIME2,
    @CreatedUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.PasswordResetTokens(Id, UserId, Token, ExpiresUtc, CreatedUtc)
    VALUES (@Id, @UserId, @Token, @ExpiresUtc, @CreatedUtc);
END
GO

-- ─── PasswordResetTokens_GetByToken ──────────────────────────────────────────
CREATE PROCEDURE dbo.PasswordResetTokens_GetByToken
    @Token NVARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, UserId, Token, ExpiresUtc, UsedUtc, CreatedUtc
    FROM dbo.PasswordResetTokens
    WHERE Token = @Token;
END
GO

-- ─── PasswordResetTokens_MarkUsed ────────────────────────────────────────────
CREATE PROCEDURE dbo.PasswordResetTokens_MarkUsed
    @Id     UNIQUEIDENTIFIER,
    @UsedUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.PasswordResetTokens SET UsedUtc = @UsedUtc WHERE Id = @Id;
END
GO

-- ─── Tenants_GetByEmail (nieuw) ───────────────────────────────────────────────
CREATE PROCEDURE dbo.Tenants_GetByEmail
    @Email NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 Id, Name, CustomerCode, DefaultIban, IsActive, CreatedUtc, ModifiedUtc,
                 CompanyName, ContactName, Email, Phone, Status,
                 TrialStartsUtc, TrialEndsUtc, GoDutchEnabled, MyPosEnabled,
                 Address, PostalCode, City, KvkNumber, OnboardingCompletedUtc
    FROM dbo.Tenants
    WHERE Email = @Email AND IsActive = 1;
END
GO
