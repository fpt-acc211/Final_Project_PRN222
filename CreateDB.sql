-- One-file bootstrap for a fresh/demo database.
-- Safe rerun rules:
--   * Missing database: create database and the current schema.
--   * Existing current schema: make no changes.
--   * Existing legacy/partial schema: stop. This script never drops data or guesses an upgrade path.
USE [master];
GO

IF DB_ID(N'QuizManagementDB') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [QuizManagementDB];');
    PRINT N'Created database QuizManagementDB.';
END
ELSE
    PRINT N'Database QuizManagementDB already exists; validating schema.';
GO

USE [QuizManagementDB];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
BEGIN
    DECLARE @MissingRequiredObjects TABLE (ObjectName NVARCHAR(256) NOT NULL);

    INSERT INTO @MissingRequiredObjects
    SELECT N'Table dbo.' + expected.Name
    FROM (VALUES
        (N'Users'), (N'Subjects'), (N'Decks'), (N'Questions'), (N'Answers'),
        (N'QuizAttempts'), (N'TestHistories'), (N'TestResultDetails'),
        (N'QuestionReports'), (N'LoginAttempts'), (N'FlashcardProgresses')) expected(Name)
    WHERE OBJECT_ID(N'dbo.' + expected.Name, N'U') IS NULL;

    IF COL_LENGTH(N'dbo.TestHistories', N'QuizAttemptId') IS NULL INSERT INTO @MissingRequiredObjects VALUES (N'Column dbo.TestHistories.QuizAttemptId');
    IF COL_LENGTH(N'dbo.TestHistories', N'ResultSnapshotJson') IS NULL INSERT INTO @MissingRequiredObjects VALUES (N'Column dbo.TestHistories.ResultSnapshotJson');
    IF COL_LENGTH(N'dbo.Users', N'NormalizedEmail') IS NULL INSERT INTO @MissingRequiredObjects VALUES (N'Column dbo.Users.NormalizedEmail');
    IF COL_LENGTH(N'dbo.Users', N'NormalizedUsername') IS NULL INSERT INTO @MissingRequiredObjects VALUES (N'Column dbo.Users.NormalizedUsername');
    IF COL_LENGTH(N'dbo.Users', N'EmailConfirmed') IS NULL INSERT INTO @MissingRequiredObjects VALUES (N'Column dbo.Users.EmailConfirmed');
    IF COL_LENGTH(N'dbo.Decks', N'NormalizedName') IS NULL INSERT INTO @MissingRequiredObjects VALUES (N'Column dbo.Decks.NormalizedName');
    IF COL_LENGTH(N'dbo.Questions', N'RowVersion') IS NULL INSERT INTO @MissingRequiredObjects VALUES (N'Column dbo.Questions.RowVersion');
    IF COL_LENGTH(N'dbo.LoginAttempts', N'CountsTowardLockout') IS NULL INSERT INTO @MissingRequiredObjects VALUES (N'Column dbo.LoginAttempts.CountsTowardLockout');

    INSERT INTO @MissingRequiredObjects
    SELECT N'Index dbo.' + expected.TableName + N'.' + expected.IndexName
    FROM (VALUES
        (N'TestHistories', N'UX_TestHistories_QuizAttemptId'),
        (N'Users', N'UX_Users_NormalizedEmail'),
        (N'Users', N'UX_Users_NormalizedUsername'),
        (N'Decks', N'UX_Decks_SubjectId_NormalizedName_Active'),
        (N'QuestionReports', N'UX_QuestionReports_QuestionId_UserId_Pending'),
        (N'Questions', N'IX_Questions_DeckId_CreatedAt'),
        (N'TestHistories', N'IX_TestHistories_UserId_CreatedAt'),
        (N'TestHistories', N'IX_TestHistories_DeckId_Percentage_CreatedAt'),
        (N'QuestionReports', N'IX_QuestionReports_IsResolved_CreatedAt'),
        (N'LoginAttempts', N'IX_LoginAttempts_IsSuccess_CreatedAt'),
        (N'LoginAttempts', N'IX_LoginAttempts_Email_IpAddress_CreatedAt'),
        (N'FlashcardProgresses', N'UX_FlashcardProgresses_UserId_QuestionId'),
        (N'FlashcardProgresses', N'IX_FlashcardProgresses_UserId_NextReviewAtUtc')) expected(TableName, IndexName)
    WHERE NOT EXISTS (
        SELECT 1
        FROM sys.indexes actual
        WHERE actual.object_id = OBJECT_ID(N'dbo.' + expected.TableName)
          AND actual.name = expected.IndexName);

    INSERT INTO @MissingRequiredObjects
    SELECT N'Constraint ' + expected.Name
    FROM (VALUES
        (N'CK_QuizAttempts_QuestionIdsJson'),
        (N'CK_QuizAttempts_TimeLimit'),
        (N'CK_QuizAttempts_Expiry'),
        (N'CK_QuizAttempts_Completed'),
        (N'CK_TestHistories_ResultSnapshotJson'),
        (N'CK_Users_Role'),
        (N'CK_Decks_TimeLimitMinutes'),
        (N'CK_Questions_QuestionType'),
        (N'CK_TestHistories_Score'),
        (N'CK_TestHistories_Percentage'),
        (N'CK_QuestionReports_Reason'),
        (N'CK_FlashcardProgresses_Repetition'),
        (N'CK_FlashcardProgresses_IntervalMinutes'),
        (N'CK_FlashcardProgresses_EaseFactor')) expected(Name)
    WHERE NOT EXISTS (
        SELECT 1
        FROM sys.check_constraints actual
        WHERE actual.name = expected.Name
          AND actual.is_disabled = 0
          AND actual.is_not_trusted = 0);

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id IN (OBJECT_ID(N'dbo.QuestionReports'), OBJECT_ID(N'dbo.LoginAttempts'))
          AND name = N'CreatedAt'
          AND TYPE_NAME(system_type_id) <> N'datetime2')
        INSERT INTO @MissingRequiredObjects VALUES (N'QuestionReports/LoginAttempts CreatedAt DATETIME2');

    IF EXISTS (SELECT 1 FROM @MissingRequiredObjects)
    BEGIN
        SELECT ObjectName AS MissingRequiredObject
        FROM @MissingRequiredObjects
        ORDER BY ObjectName;

        THROW 51020, 'QuizManagementDB does not match the current schema. For a disposable demo database, drop it manually and rerun CreateDB.sql. No changes were made.', 1;
    END;

    PRINT N'QuizManagementDB already matches the current schema; no changes were made.';
    RETURN;
END;

IF EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0)
    THROW 51021, 'QuizManagementDB contains a partial or unrecognized schema. No changes were made; use a clean database.', 1;

BEGIN TRY
BEGIN TRANSACTION;

-- 1. Bảng Users
CREATE TABLE Users (
    Id NVARCHAR(450) PRIMARY KEY, -- Khớp với kiểu Id mặc định của ASP.NET Core Identity
    Username NVARCHAR(256) NOT NULL,
    Email NVARCHAR(256) NOT NULL,
    NormalizedUsername AS UPPER(LTRIM(RTRIM(Username))) PERSISTED,
    NormalizedEmail AS UPPER(LTRIM(RTRIM(Email))) PERSISTED,
    PasswordHash NVARCHAR(MAX),
    Role NVARCHAR(50),
    -- Role/Profile/Security fields (merged from Phase 2)
    AvatarUrl NVARCHAR(500) NULL,
    IsDisabled BIT NOT NULL DEFAULT 0,
    EmailConfirmed BIT NOT NULL DEFAULT 0,
    SecurityStamp NVARCHAR(450) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT CK_Users_Role CHECK (Role IS NULL OR Role IN (N'Admin', N'Mentor', N'User'))
);
CREATE UNIQUE INDEX UX_Users_NormalizedUsername ON Users(NormalizedUsername);
CREATE UNIQUE INDEX UX_Users_NormalizedEmail ON Users(NormalizedEmail);

-- 2. Bảng Subjects
CREATE TABLE Subjects (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(256) NULL,
    UpdatedBy NVARCHAR(256) NULL,
    CONSTRAINT FK_Subjects_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);
-- Tạo Filtered Index để tránh trùng tên môn học nhưng bỏ qua các môn đã bị xóa (Soft Delete)
CREATE UNIQUE INDEX IX_Subjects_Name_UserId ON Subjects(Name, UserId) WHERE IsDeleted = 0;

-- 3. Bảng Decks
CREATE TABLE Decks (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SubjectId INT NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    NormalizedName AS UPPER(LTRIM(RTRIM(Name))) PERSISTED,
    TimeLimitMinutes INT NOT NULL DEFAULT 0, -- 0 = không giới hạn; > 0 = số phút; do Mentor đặt
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(256) NULL,
    UpdatedBy NVARCHAR(256) NULL,
    CONSTRAINT FK_Decks_Subjects FOREIGN KEY (SubjectId) REFERENCES Subjects(Id) ON DELETE CASCADE,
    CONSTRAINT CK_Decks_TimeLimitMinutes CHECK (TimeLimitMinutes BETWEEN 0 AND 180)
);
CREATE UNIQUE INDEX UX_Decks_SubjectId_NormalizedName_Active
    ON Decks(SubjectId, NormalizedName) WHERE IsDeleted = 0;

-- 4. Bảng Questions
CREATE TABLE Questions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DeckId INT NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Explanation NVARCHAR(MAX) NULL,
    QuestionType INT NOT NULL DEFAULT 1, -- 1: SingleChoice, 2: MultipleChoice
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(256) NULL,
    UpdatedBy NVARCHAR(256) NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_Questions_Decks FOREIGN KEY (DeckId) REFERENCES Decks(Id) ON DELETE CASCADE,
    CONSTRAINT CK_Questions_QuestionType CHECK (QuestionType IN (1, 2))
);
CREATE INDEX IX_Questions_DeckId_CreatedAt
    ON Questions(DeckId, CreatedAt DESC) WHERE IsDeleted = 0;

-- 5. Bảng Answers
CREATE TABLE Answers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuestionId INT NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    IsCorrect BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Answers_Questions FOREIGN KEY (QuestionId) REFERENCES Questions(Id) ON DELETE CASCADE
);

-- 6. Bảng QuizAttempts — server-side state cho timeout/idempotency
CREATE TABLE QuizAttempts (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    DeckId INT NOT NULL,
    QuestionIdsJson NVARCHAR(MAX) NOT NULL,
    TimeLimitMinutes INT NOT NULL,
    StartedAtUtc DATETIMEOFFSET(7) NOT NULL,
    ExpiresAtUtc DATETIMEOFFSET(7) NULL,
    CompletedAtUtc DATETIMEOFFSET(7) NULL,
    CONSTRAINT FK_QuizAttempts_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_QuizAttempts_Decks FOREIGN KEY (DeckId) REFERENCES Decks(Id),
    CONSTRAINT CK_QuizAttempts_QuestionIdsJson CHECK (ISJSON(QuestionIdsJson) = 1),
    CONSTRAINT CK_QuizAttempts_TimeLimit CHECK (TimeLimitMinutes BETWEEN 0 AND 180),
    CONSTRAINT CK_QuizAttempts_Expiry CHECK (
        (TimeLimitMinutes = 0 AND ExpiresAtUtc IS NULL) OR
        (TimeLimitMinutes > 0 AND ExpiresAtUtc > StartedAtUtc)
    ),
    CONSTRAINT CK_QuizAttempts_Completed CHECK (CompletedAtUtc IS NULL OR CompletedAtUtc >= StartedAtUtc)
);
CREATE INDEX IX_QuizAttempts_UserId_StartedAtUtc ON QuizAttempts(UserId, StartedAtUtc DESC);
CREATE INDEX IX_QuizAttempts_Pending ON QuizAttempts(CompletedAtUtc) WHERE CompletedAtUtc IS NULL;

-- 7. Bảng TestHistories
CREATE TABLE TestHistories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    DeckId INT NOT NULL,
    QuizAttemptId UNIQUEIDENTIFIER NULL,
    ResultSnapshotJson NVARCHAR(MAX) NULL,
    Score FLOAT NOT NULL,
    Percentage FLOAT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_TestHistories_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_TestHistories_Decks FOREIGN KEY (DeckId) REFERENCES Decks(Id),
    CONSTRAINT FK_TestHistories_QuizAttempts FOREIGN KEY (QuizAttemptId) REFERENCES QuizAttempts(Id),
    CONSTRAINT CK_TestHistories_ResultSnapshotJson CHECK (ResultSnapshotJson IS NULL OR ISJSON(ResultSnapshotJson) = 1),
    CONSTRAINT CK_TestHistories_Score CHECK (Score BETWEEN 0 AND 10),
    CONSTRAINT CK_TestHistories_Percentage CHECK (Percentage BETWEEN 0 AND 100)
);
CREATE UNIQUE INDEX UX_TestHistories_QuizAttemptId ON TestHistories(QuizAttemptId) WHERE QuizAttemptId IS NOT NULL;
CREATE INDEX IX_TestHistories_UserId_CreatedAt ON TestHistories(UserId, CreatedAt DESC);
CREATE INDEX IX_TestHistories_DeckId_Percentage_CreatedAt
    ON TestHistories(DeckId, Percentage DESC, CreatedAt DESC);

-- 8. Bảng TestResultDetails
CREATE TABLE TestResultDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TestHistoryId INT NOT NULL,
    QuestionId INT NOT NULL,
    SelectedAnswerId INT NULL,
    IsCorrect BIT NOT NULL,
    CONSTRAINT FK_Details_History FOREIGN KEY (TestHistoryId) REFERENCES TestHistories(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Details_Question FOREIGN KEY (QuestionId) REFERENCES Questions(Id),
    CONSTRAINT FK_Details_Answer FOREIGN KEY (SelectedAnswerId) REFERENCES Answers(Id)
);

-- 9. Bảng QuestionReports
-- Người dùng báo cáo câu hỏi sai/không rõ; Mentor/Admin xem và xử lý
CREATE TABLE QuestionReports (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuestionId INT NOT NULL,
    UserId NVARCHAR(450) NOT NULL,
    Reason NVARCHAR(100) NOT NULL,           -- WrongAnswer | UnclearQuestion | DuplicateQuestion | Other
    Note NVARCHAR(500) NULL,                 -- Ghi chú thêm của người báo cáo
    IsResolved BIT NOT NULL DEFAULT 0,       -- Mentor/Admin đánh dấu đã xử lý
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_QuestionReports_Questions FOREIGN KEY (QuestionId) REFERENCES Questions(Id),
    CONSTRAINT FK_QuestionReports_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT CK_QuestionReports_Reason CHECK (Reason IN (N'WrongAnswer', N'UnclearQuestion', N'DuplicateQuestion', N'Other'))
);
CREATE UNIQUE INDEX UX_QuestionReports_QuestionId_UserId_Pending
    ON QuestionReports(QuestionId, UserId) WHERE IsResolved = 0;
CREATE INDEX IX_QuestionReports_IsResolved_CreatedAt
    ON QuestionReports(IsResolved, CreatedAt DESC);

-- 10. Bảng LoginAttempts
-- Ghi lại mọi lần đăng nhập (thành công và thất bại) để Admin theo dõi
CREATE TABLE LoginAttempts (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(256) NOT NULL,
    IpAddress NVARCHAR(50) NOT NULL,
    IsSuccess BIT NOT NULL,
    CountsTowardLockout BIT NOT NULL DEFAULT 0,
    UserId NVARCHAR(450) NULL,               -- NULL nếu email không tồn tại
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_LoginAttempts_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);
CREATE INDEX IX_LoginAttempts_IsSuccess_CreatedAt
    ON LoginAttempts(IsSuccess, CreatedAt DESC);
CREATE INDEX IX_LoginAttempts_Email_IpAddress_CreatedAt
    ON LoginAttempts(Email, IpAddress, CreatedAt DESC);

-- 11. Tiến độ spaced repetition của từng người dùng/câu hỏi
CREATE TABLE FlashcardProgresses (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    QuestionId INT NOT NULL,
    Repetition INT NOT NULL DEFAULT 0,
    IntervalMinutes INT NOT NULL DEFAULT 10,
    EaseFactor FLOAT NOT NULL DEFAULT 2.5,
    LastReviewedAtUtc DATETIME2(7) NOT NULL,
    NextReviewAtUtc DATETIME2(7) NOT NULL,
    CONSTRAINT FK_FlashcardProgresses_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_FlashcardProgresses_Questions FOREIGN KEY (QuestionId) REFERENCES Questions(Id) ON DELETE CASCADE,
    CONSTRAINT CK_FlashcardProgresses_Repetition CHECK (Repetition >= 0),
    CONSTRAINT CK_FlashcardProgresses_IntervalMinutes CHECK (IntervalMinutes >= 1),
    CONSTRAINT CK_FlashcardProgresses_EaseFactor CHECK (EaseFactor BETWEEN 1.3 AND 3.0)
);
CREATE UNIQUE INDEX UX_FlashcardProgresses_UserId_QuestionId
    ON FlashcardProgresses(UserId, QuestionId);
CREATE INDEX IX_FlashcardProgresses_UserId_NextReviewAtUtc
    ON FlashcardProgresses(UserId, NextReviewAtUtc);

COMMIT TRANSACTION;
PRINT N'QuizManagementDB schema was created successfully.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
