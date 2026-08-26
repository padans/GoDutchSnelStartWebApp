-- RequirePasswordChange kolom toevoegen aan AppUsers
ALTER TABLE dbo.AppUsers ADD RequirePasswordChange BIT NOT NULL DEFAULT 0;
GO

-- Alle gebruikers ophalen
CREATE OR ALTER PROCEDURE dbo.AppUsers_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Username, PasswordHash, Module, IsActive, CreatedUtc, RequirePasswordChange
    FROM dbo.AppUsers
    ORDER BY Username;
END
GO

-- Gebruiker ophalen op Id
CREATE OR ALTER PROCEDURE dbo.AppUsers_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Username, PasswordHash, Module, IsActive, CreatedUtc, RequirePasswordChange
    FROM dbo.AppUsers
    WHERE Id = @Id;
END
GO

-- Gebruiker ophalen op gebruikersnaam
CREATE OR ALTER PROCEDURE dbo.AppUsers_GetByUsername
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Username, PasswordHash, Module, IsActive, CreatedUtc, RequirePasswordChange
    FROM dbo.AppUsers
    WHERE Username = @Username;
END
GO

-- Gebruiker aanmaken
CREATE OR ALTER PROCEDURE dbo.AppUsers_Insert
    @Id                    UNIQUEIDENTIFIER,
    @Username              NVARCHAR(100),
    @PasswordHash          NVARCHAR(500),
    @Module                NVARCHAR(50),
    @IsActive              BIT,
    @CreatedUtc            DATETIME2,
    @RequirePasswordChange BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.AppUsers (Id, Username, PasswordHash, Module, IsActive, CreatedUtc, RequirePasswordChange)
    VALUES (@Id, @Username, @PasswordHash, @Module, @IsActive, @CreatedUtc, @RequirePasswordChange);
END
GO

-- Gebruiker bijwerken
CREATE OR ALTER PROCEDURE dbo.AppUsers_Update
    @Id                    UNIQUEIDENTIFIER,
    @Username              NVARCHAR(100),
    @PasswordHash          NVARCHAR(500),
    @Module                NVARCHAR(50),
    @IsActive              BIT,
    @CreatedUtc            DATETIME2,
    @RequirePasswordChange BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.AppUsers
    SET Username              = @Username,
        PasswordHash          = @PasswordHash,
        Module                = @Module,
        IsActive              = @IsActive,
        RequirePasswordChange = @RequirePasswordChange
    WHERE Id = @Id;
END
GO
