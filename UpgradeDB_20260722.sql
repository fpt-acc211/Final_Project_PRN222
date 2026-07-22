-- Safe, idempotent upgrade from the previous final-submission schema.
USE [QuizManagementDB];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
    THROW 51022, 'QuizManagementDB base schema is missing. Run CreateDB.sql on a clean database instead.', 1;

BEGIN TRY
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.Users', N'EmailConfirmed') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD EmailConfirmed BIT NULL;
    EXEC(N'UPDATE dbo.Users SET EmailConfirmed = 1;
        ALTER TABLE dbo.Users ALTER COLUMN EmailConfirmed BIT NOT NULL;
        ALTER TABLE dbo.Users ADD CONSTRAINT DF_Users_EmailConfirmed DEFAULT 0 FOR EmailConfirmed;');
END;

IF COL_LENGTH(N'dbo.LoginAttempts', N'CountsTowardLockout') IS NULL
    ALTER TABLE dbo.LoginAttempts
        ADD CountsTowardLockout BIT NOT NULL
            CONSTRAINT DF_LoginAttempts_CountsTowardLockout DEFAULT 0 WITH VALUES;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.LoginAttempts')
      AND name = N'IX_LoginAttempts_Email_IpAddress_CreatedAt')
    CREATE INDEX IX_LoginAttempts_Email_IpAddress_CreatedAt
        ON dbo.LoginAttempts(Email, IpAddress, CreatedAt DESC);

IF OBJECT_ID(N'dbo.FlashcardProgresses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FlashcardProgresses (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId NVARCHAR(450) NOT NULL,
        QuestionId INT NOT NULL,
        Repetition INT NOT NULL DEFAULT 0,
        IntervalMinutes INT NOT NULL DEFAULT 10,
        EaseFactor FLOAT NOT NULL DEFAULT 2.5,
        LastReviewedAtUtc DATETIME2(7) NOT NULL,
        NextReviewAtUtc DATETIME2(7) NOT NULL,
        CONSTRAINT FK_FlashcardProgresses_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
        CONSTRAINT FK_FlashcardProgresses_Questions FOREIGN KEY (QuestionId) REFERENCES dbo.Questions(Id) ON DELETE CASCADE,
        CONSTRAINT CK_FlashcardProgresses_Repetition CHECK (Repetition >= 0),
        CONSTRAINT CK_FlashcardProgresses_IntervalMinutes CHECK (IntervalMinutes >= 1),
        CONSTRAINT CK_FlashcardProgresses_EaseFactor CHECK (EaseFactor BETWEEN 1.3 AND 3.0)
    );
    CREATE UNIQUE INDEX UX_FlashcardProgresses_UserId_QuestionId
        ON dbo.FlashcardProgresses(UserId, QuestionId);
    CREATE INDEX IX_FlashcardProgresses_UserId_NextReviewAtUtc
        ON dbo.FlashcardProgresses(UserId, NextReviewAtUtc);
END;

COMMIT TRANSACTION;
PRINT N'QuizManagementDB upgrade completed successfully.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
