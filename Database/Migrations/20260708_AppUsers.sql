-- AppUsers tabel aanmaken
CREATE TABLE dbo.AppUsers (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    Username        NVARCHAR(100)    NOT NULL,
    PasswordHash    NVARCHAR(500)    NOT NULL,
    Module          NVARCHAR(50)     NOT NULL,
    IsActive        BIT              NOT NULL DEFAULT 1,
    CreatedUtc      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_AppUsers PRIMARY KEY (Id),
    CONSTRAINT UQ_AppUsers_Username UNIQUE (Username)
);
GO

-- Alle gebruikers ophalen
CREATE OR ALTER PROCEDURE dbo.AppUsers_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Username, PasswordHash, Module, IsActive, CreatedUtc
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
    SELECT Id, Username, PasswordHash, Module, IsActive, CreatedUtc
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
    SELECT Id, Username, PasswordHash, Module, IsActive, CreatedUtc
    FROM dbo.AppUsers
    WHERE Username = @Username;
END
GO

-- Gebruiker aanmaken
CREATE OR ALTER PROCEDURE dbo.AppUsers_Insert
    @Id           UNIQUEIDENTIFIER,
    @Username     NVARCHAR(100),
    @PasswordHash NVARCHAR(500),
    @Module       NVARCHAR(50),
    @IsActive     BIT,
    @CreatedUtc   DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.AppUsers (Id, Username, PasswordHash, Module, IsActive, CreatedUtc)
    VALUES (@Id, @Username, @PasswordHash, @Module, @IsActive, @CreatedUtc);
END
GO

-- Gebruiker bijwerken
CREATE OR ALTER PROCEDURE dbo.AppUsers_Update
    @Id           UNIQUEIDENTIFIER,
    @Username     NVARCHAR(100),
    @PasswordHash NVARCHAR(500),
    @Module       NVARCHAR(50),
    @IsActive     BIT,
    @CreatedUtc   DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.AppUsers
    SET Username     = @Username,
        PasswordHash = @PasswordHash,
        Module       = @Module,
        IsActive     = @IsActive
    WHERE Id = @Id;
END
GO

-- Gebruiker verwijderen
CREATE OR ALTER PROCEDURE dbo.AppUsers_Delete
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.AppUsers WHERE Id = @Id;
END
GO
