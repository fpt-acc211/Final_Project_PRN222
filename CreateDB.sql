CREATE DATABASE QuizManagementDB;
GO
USE QuizManagementDB;
GO

-- 1. Bảng Users
CREATE TABLE Users (
    Id NVARCHAR(450) PRIMARY KEY, -- Khớp với kiểu Id mặc định của ASP.NET Core Identity
    Username NVARCHAR(256) NOT NULL,
    Email NVARCHAR(256) NOT NULL,
    PasswordHash NVARCHAR(MAX),
    Role NVARCHAR(50),
    -- Role/Profile/Security fields (merged from Phase 2)
    AvatarUrl NVARCHAR(500) NULL,
    IsDisabled BIT NOT NULL DEFAULT 0,
    SecurityStamp NVARCHAR(450) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);

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
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(256) NULL,
    UpdatedBy NVARCHAR(256) NULL,
    CONSTRAINT FK_Decks_Subjects FOREIGN KEY (SubjectId) REFERENCES Subjects(Id) ON DELETE CASCADE
);

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
    CONSTRAINT FK_Questions_Decks FOREIGN KEY (DeckId) REFERENCES Decks(Id) ON DELETE CASCADE
);

-- 5. Bảng Answers
CREATE TABLE Answers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuestionId INT NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    IsCorrect BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Answers_Questions FOREIGN KEY (QuestionId) REFERENCES Questions(Id) ON DELETE CASCADE
);

-- 6. Bảng TestHistories
CREATE TABLE TestHistories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    DeckId INT NOT NULL,
    Score FLOAT NOT NULL,
    Percentage FLOAT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_TestHistories_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_TestHistories_Decks FOREIGN KEY (DeckId) REFERENCES Decks(Id)
);

-- 7. Bảng TestResultDetails
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
GO
