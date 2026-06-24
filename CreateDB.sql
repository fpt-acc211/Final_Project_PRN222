IF DB_ID(N'QuizManagementDB') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [QuizManagementDB]');
    PRINT N'Created database QuizManagementDB';
END
ELSE
BEGIN
    PRINT N'Database QuizManagementDB already exists';
END;
GO

USE [QuizManagementDB];
GO

SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    -- This script is idempotent: run it on a fresh machine to create the schema,
    -- or run it on an existing database to add missing tables/columns/constraints.

    ---------------------------------------------------------------------------
    -- 1. Users
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Users (
            Id NVARCHAR(450) NOT NULL PRIMARY KEY,
            Username NVARCHAR(256) NOT NULL,
            Email NVARCHAR(256) NOT NULL,
            PasswordHash NVARCHAR(MAX) NULL,
            Role NVARCHAR(50) NULL,
            AvatarUrl NVARCHAR(500) NULL,
            IsDisabled BIT NOT NULL CONSTRAINT DF_Users_IsDisabled DEFAULT (0),
            SecurityStamp NVARCHAR(450) NULL,
            CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (GETUTCDATE()),
            UpdatedAt DATETIME2 NULL
        );

        PRINT N'Created Users table';
    END
    ELSE
    BEGIN
        IF COL_LENGTH(N'dbo.Users', N'Username') IS NULL
            ALTER TABLE dbo.Users ADD Username NVARCHAR(256) NOT NULL CONSTRAINT DF_Users_Username DEFAULT (N'');

        IF COL_LENGTH(N'dbo.Users', N'Email') IS NULL
            ALTER TABLE dbo.Users ADD Email NVARCHAR(256) NOT NULL CONSTRAINT DF_Users_Email DEFAULT (N'');

        IF COL_LENGTH(N'dbo.Users', N'PasswordHash') IS NULL
            ALTER TABLE dbo.Users ADD PasswordHash NVARCHAR(MAX) NULL;

        IF COL_LENGTH(N'dbo.Users', N'Role') IS NULL
            ALTER TABLE dbo.Users ADD Role NVARCHAR(50) NULL;

        IF COL_LENGTH(N'dbo.Users', N'AvatarUrl') IS NULL
            ALTER TABLE dbo.Users ADD AvatarUrl NVARCHAR(500) NULL;

        IF COL_LENGTH(N'dbo.Users', N'IsDisabled') IS NULL
            ALTER TABLE dbo.Users ADD IsDisabled BIT NOT NULL CONSTRAINT DF_Users_IsDisabled DEFAULT (0);

        IF COL_LENGTH(N'dbo.Users', N'SecurityStamp') IS NULL
            ALTER TABLE dbo.Users ADD SecurityStamp NVARCHAR(450) NULL;

        IF COL_LENGTH(N'dbo.Users', N'CreatedAt') IS NULL
            ALTER TABLE dbo.Users ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (GETUTCDATE());

        IF COL_LENGTH(N'dbo.Users', N'UpdatedAt') IS NULL
            ALTER TABLE dbo.Users ADD UpdatedAt DATETIME2 NULL;
    END;

    IF COL_LENGTH(N'dbo.Users', N'IsDisabled') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Users')
              AND c.name = N'IsDisabled'
       )
    BEGIN
        EXEC(N'UPDATE dbo.Users SET IsDisabled = 0 WHERE IsDisabled IS NULL;');
        EXEC(N'ALTER TABLE dbo.Users ADD CONSTRAINT DF_Users_IsDisabled DEFAULT (0) FOR IsDisabled;');
    END;

    IF COL_LENGTH(N'dbo.Users', N'CreatedAt') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Users')
              AND c.name = N'CreatedAt'
       )
    BEGIN
        EXEC(N'UPDATE dbo.Users SET CreatedAt = GETUTCDATE() WHERE CreatedAt IS NULL;');
        EXEC(N'ALTER TABLE dbo.Users ADD CONSTRAINT DF_Users_CreatedAt DEFAULT (GETUTCDATE()) FOR CreatedAt;');
    END;

    ---------------------------------------------------------------------------
    -- 2. Subjects
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.Subjects', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Subjects (
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            UserId NVARCHAR(450) NOT NULL,
            Name NVARCHAR(255) NOT NULL,
            IsDeleted BIT NOT NULL CONSTRAINT DF_Subjects_IsDeleted DEFAULT (0),
            CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Subjects_CreatedAt DEFAULT (GETUTCDATE()),
            UpdatedAt DATETIME2 NULL,
            CreatedBy NVARCHAR(256) NULL,
            UpdatedBy NVARCHAR(256) NULL
        );

        PRINT N'Created Subjects table';
    END
    ELSE
    BEGIN
        IF COL_LENGTH(N'dbo.Subjects', N'UserId') IS NULL
            ALTER TABLE dbo.Subjects ADD UserId NVARCHAR(450) NOT NULL CONSTRAINT DF_Subjects_UserId DEFAULT (N'');

        IF COL_LENGTH(N'dbo.Subjects', N'Name') IS NULL
            ALTER TABLE dbo.Subjects ADD Name NVARCHAR(255) NOT NULL CONSTRAINT DF_Subjects_Name DEFAULT (N'');

        IF COL_LENGTH(N'dbo.Subjects', N'IsDeleted') IS NULL
            ALTER TABLE dbo.Subjects ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Subjects_IsDeleted DEFAULT (0);

        IF COL_LENGTH(N'dbo.Subjects', N'CreatedAt') IS NULL
            ALTER TABLE dbo.Subjects ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Subjects_CreatedAt DEFAULT (GETUTCDATE());

        IF COL_LENGTH(N'dbo.Subjects', N'UpdatedAt') IS NULL
            ALTER TABLE dbo.Subjects ADD UpdatedAt DATETIME2 NULL;

        IF COL_LENGTH(N'dbo.Subjects', N'CreatedBy') IS NULL
            ALTER TABLE dbo.Subjects ADD CreatedBy NVARCHAR(256) NULL;

        IF COL_LENGTH(N'dbo.Subjects', N'UpdatedBy') IS NULL
            ALTER TABLE dbo.Subjects ADD UpdatedBy NVARCHAR(256) NULL;
    END;

    IF COL_LENGTH(N'dbo.Subjects', N'IsDeleted') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Subjects')
              AND c.name = N'IsDeleted'
       )
    BEGIN
        EXEC(N'UPDATE dbo.Subjects SET IsDeleted = 0 WHERE IsDeleted IS NULL;');
        EXEC(N'ALTER TABLE dbo.Subjects ADD CONSTRAINT DF_Subjects_IsDeleted DEFAULT (0) FOR IsDeleted;');
    END;

    IF COL_LENGTH(N'dbo.Subjects', N'CreatedAt') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Subjects')
              AND c.name = N'CreatedAt'
       )
    BEGIN
        EXEC(N'UPDATE dbo.Subjects SET CreatedAt = GETUTCDATE() WHERE CreatedAt IS NULL;');
        EXEC(N'ALTER TABLE dbo.Subjects ADD CONSTRAINT DF_Subjects_CreatedAt DEFAULT (GETUTCDATE()) FOR CreatedAt;');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_Subjects_Users'
          AND parent_object_id = OBJECT_ID(N'dbo.Subjects')
    )
    BEGIN
        EXEC(N'ALTER TABLE dbo.Subjects WITH CHECK
        ADD CONSTRAINT FK_Subjects_Users
            FOREIGN KEY (UserId) REFERENCES dbo.Users(Id);');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Subjects')
          AND name = N'IX_Subjects_Name_UserId'
    )
    BEGIN
        EXEC(N'IF NOT EXISTS (
            SELECT 1
            FROM dbo.Subjects
            WHERE IsDeleted = 0
            GROUP BY Name, UserId
            HAVING COUNT(*) > 1
        )
        BEGIN
            CREATE UNIQUE INDEX IX_Subjects_Name_UserId
            ON dbo.Subjects(Name, UserId)
            WHERE IsDeleted = 0;
        END
        ELSE
        BEGIN
            PRINT N''Skipped IX_Subjects_Name_UserId because duplicate active subjects already exist.'';
        END;');
    END;

    ---------------------------------------------------------------------------
    -- 3. Decks
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.Decks', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Decks (
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            SubjectId INT NOT NULL,
            Name NVARCHAR(255) NOT NULL,
            TimeLimitMinutes INT NOT NULL CONSTRAINT DF_Decks_TimeLimitMinutes DEFAULT (0),
            IsDeleted BIT NOT NULL CONSTRAINT DF_Decks_IsDeleted DEFAULT (0),
            CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Decks_CreatedAt DEFAULT (GETUTCDATE()),
            UpdatedAt DATETIME2 NULL,
            CreatedBy NVARCHAR(256) NULL,
            UpdatedBy NVARCHAR(256) NULL
        );

        PRINT N'Created Decks table';
    END
    ELSE
    BEGIN
        IF COL_LENGTH(N'dbo.Decks', N'SubjectId') IS NULL
            ALTER TABLE dbo.Decks ADD SubjectId INT NOT NULL CONSTRAINT DF_Decks_SubjectId DEFAULT (0);

        IF COL_LENGTH(N'dbo.Decks', N'Name') IS NULL
            ALTER TABLE dbo.Decks ADD Name NVARCHAR(255) NOT NULL CONSTRAINT DF_Decks_Name DEFAULT (N'');

        IF COL_LENGTH(N'dbo.Decks', N'TimeLimitMinutes') IS NULL
            ALTER TABLE dbo.Decks ADD TimeLimitMinutes INT NOT NULL CONSTRAINT DF_Decks_TimeLimitMinutes DEFAULT (0);

        IF COL_LENGTH(N'dbo.Decks', N'IsDeleted') IS NULL
            ALTER TABLE dbo.Decks ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Decks_IsDeleted DEFAULT (0);

        IF COL_LENGTH(N'dbo.Decks', N'CreatedAt') IS NULL
            ALTER TABLE dbo.Decks ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Decks_CreatedAt DEFAULT (GETUTCDATE());

        IF COL_LENGTH(N'dbo.Decks', N'UpdatedAt') IS NULL
            ALTER TABLE dbo.Decks ADD UpdatedAt DATETIME2 NULL;

        IF COL_LENGTH(N'dbo.Decks', N'CreatedBy') IS NULL
            ALTER TABLE dbo.Decks ADD CreatedBy NVARCHAR(256) NULL;

        IF COL_LENGTH(N'dbo.Decks', N'UpdatedBy') IS NULL
            ALTER TABLE dbo.Decks ADD UpdatedBy NVARCHAR(256) NULL;
    END;

    IF COL_LENGTH(N'dbo.Decks', N'TimeLimitMinutes') IS NOT NULL
       AND EXISTS (
            SELECT 1
            FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.Decks')
              AND name = N'TimeLimitMinutes'
              AND is_nullable = 1
       )
    BEGIN
        EXEC(N'UPDATE dbo.Decks SET TimeLimitMinutes = 0 WHERE TimeLimitMinutes IS NULL;');
        EXEC(N'ALTER TABLE dbo.Decks ALTER COLUMN TimeLimitMinutes INT NOT NULL;');
    END;

    IF COL_LENGTH(N'dbo.Decks', N'TimeLimitMinutes') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Decks')
              AND c.name = N'TimeLimitMinutes'
       )
    BEGIN
        EXEC(N'ALTER TABLE dbo.Decks ADD CONSTRAINT DF_Decks_TimeLimitMinutes DEFAULT (0) FOR TimeLimitMinutes;');
    END;

    IF COL_LENGTH(N'dbo.Decks', N'IsDeleted') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Decks')
              AND c.name = N'IsDeleted'
       )
    BEGIN
        EXEC(N'UPDATE dbo.Decks SET IsDeleted = 0 WHERE IsDeleted IS NULL;');
        EXEC(N'ALTER TABLE dbo.Decks ADD CONSTRAINT DF_Decks_IsDeleted DEFAULT (0) FOR IsDeleted;');
    END;

    IF COL_LENGTH(N'dbo.Decks', N'CreatedAt') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Decks')
              AND c.name = N'CreatedAt'
       )
    BEGIN
        EXEC(N'UPDATE dbo.Decks SET CreatedAt = GETUTCDATE() WHERE CreatedAt IS NULL;');
        EXEC(N'ALTER TABLE dbo.Decks ADD CONSTRAINT DF_Decks_CreatedAt DEFAULT (GETUTCDATE()) FOR CreatedAt;');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_Decks_Subjects'
          AND parent_object_id = OBJECT_ID(N'dbo.Decks')
    )
    BEGIN
        EXEC(N'ALTER TABLE dbo.Decks WITH CHECK
        ADD CONSTRAINT FK_Decks_Subjects
            FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects(Id) ON DELETE CASCADE;');
    END;

    ---------------------------------------------------------------------------
    -- 4. Questions
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.Questions', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Questions (
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            DeckId INT NOT NULL,
            Content NVARCHAR(MAX) NOT NULL,
            Explanation NVARCHAR(MAX) NULL,
            QuestionType INT NOT NULL CONSTRAINT DF_Questions_QuestionType DEFAULT (1),
            IsDeleted BIT NOT NULL CONSTRAINT DF_Questions_IsDeleted DEFAULT (0),
            CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Questions_CreatedAt DEFAULT (GETUTCDATE()),
            UpdatedAt DATETIME2 NULL,
            CreatedBy NVARCHAR(256) NULL,
            UpdatedBy NVARCHAR(256) NULL
        );

        PRINT N'Created Questions table';
    END
    ELSE
    BEGIN
        IF COL_LENGTH(N'dbo.Questions', N'DeckId') IS NULL
            ALTER TABLE dbo.Questions ADD DeckId INT NOT NULL CONSTRAINT DF_Questions_DeckId DEFAULT (0);

        IF COL_LENGTH(N'dbo.Questions', N'Content') IS NULL
            ALTER TABLE dbo.Questions ADD Content NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Questions_Content DEFAULT (N'');

        IF COL_LENGTH(N'dbo.Questions', N'Explanation') IS NULL
            ALTER TABLE dbo.Questions ADD Explanation NVARCHAR(MAX) NULL;

        IF COL_LENGTH(N'dbo.Questions', N'QuestionType') IS NULL
            ALTER TABLE dbo.Questions ADD QuestionType INT NOT NULL CONSTRAINT DF_Questions_QuestionType DEFAULT (1);

        IF COL_LENGTH(N'dbo.Questions', N'IsDeleted') IS NULL
            ALTER TABLE dbo.Questions ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Questions_IsDeleted DEFAULT (0);

        IF COL_LENGTH(N'dbo.Questions', N'CreatedAt') IS NULL
            ALTER TABLE dbo.Questions ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Questions_CreatedAt DEFAULT (GETUTCDATE());

        IF COL_LENGTH(N'dbo.Questions', N'UpdatedAt') IS NULL
            ALTER TABLE dbo.Questions ADD UpdatedAt DATETIME2 NULL;

        IF COL_LENGTH(N'dbo.Questions', N'CreatedBy') IS NULL
            ALTER TABLE dbo.Questions ADD CreatedBy NVARCHAR(256) NULL;

        IF COL_LENGTH(N'dbo.Questions', N'UpdatedBy') IS NULL
            ALTER TABLE dbo.Questions ADD UpdatedBy NVARCHAR(256) NULL;
    END;

    IF COL_LENGTH(N'dbo.Questions', N'QuestionType') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Questions')
              AND c.name = N'QuestionType'
       )
    BEGIN
        EXEC(N'UPDATE dbo.Questions SET QuestionType = 1 WHERE QuestionType IS NULL;');
        EXEC(N'ALTER TABLE dbo.Questions ADD CONSTRAINT DF_Questions_QuestionType DEFAULT (1) FOR QuestionType;');
    END;

    IF COL_LENGTH(N'dbo.Questions', N'IsDeleted') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Questions')
              AND c.name = N'IsDeleted'
       )
    BEGIN
        EXEC(N'UPDATE dbo.Questions SET IsDeleted = 0 WHERE IsDeleted IS NULL;');
        EXEC(N'ALTER TABLE dbo.Questions ADD CONSTRAINT DF_Questions_IsDeleted DEFAULT (0) FOR IsDeleted;');
    END;

    IF COL_LENGTH(N'dbo.Questions', N'CreatedAt') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Questions')
              AND c.name = N'CreatedAt'
       )
    BEGIN
        EXEC(N'UPDATE dbo.Questions SET CreatedAt = GETUTCDATE() WHERE CreatedAt IS NULL;');
        EXEC(N'ALTER TABLE dbo.Questions ADD CONSTRAINT DF_Questions_CreatedAt DEFAULT (GETUTCDATE()) FOR CreatedAt;');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_Questions_Decks'
          AND parent_object_id = OBJECT_ID(N'dbo.Questions')
    )
    BEGIN
        EXEC(N'ALTER TABLE dbo.Questions WITH CHECK
        ADD CONSTRAINT FK_Questions_Decks
            FOREIGN KEY (DeckId) REFERENCES dbo.Decks(Id) ON DELETE CASCADE;');
    END;

    ---------------------------------------------------------------------------
    -- 5. Answers
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.Answers', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Answers (
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            QuestionId INT NOT NULL,
            Content NVARCHAR(MAX) NOT NULL,
            IsCorrect BIT NOT NULL CONSTRAINT DF_Answers_IsCorrect DEFAULT (0)
        );

        PRINT N'Created Answers table';
    END
    ELSE
    BEGIN
        IF COL_LENGTH(N'dbo.Answers', N'QuestionId') IS NULL
            ALTER TABLE dbo.Answers ADD QuestionId INT NOT NULL CONSTRAINT DF_Answers_QuestionId DEFAULT (0);

        IF COL_LENGTH(N'dbo.Answers', N'Content') IS NULL
            ALTER TABLE dbo.Answers ADD Content NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Answers_Content DEFAULT (N'');

        IF COL_LENGTH(N'dbo.Answers', N'IsCorrect') IS NULL
            ALTER TABLE dbo.Answers ADD IsCorrect BIT NOT NULL CONSTRAINT DF_Answers_IsCorrect DEFAULT (0);
    END;

    IF COL_LENGTH(N'dbo.Answers', N'IsCorrect') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Answers')
              AND c.name = N'IsCorrect'
       )
    BEGIN
        EXEC(N'UPDATE dbo.Answers SET IsCorrect = 0 WHERE IsCorrect IS NULL;');
        EXEC(N'ALTER TABLE dbo.Answers ADD CONSTRAINT DF_Answers_IsCorrect DEFAULT (0) FOR IsCorrect;');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_Answers_Questions'
          AND parent_object_id = OBJECT_ID(N'dbo.Answers')
    )
    BEGIN
        EXEC(N'ALTER TABLE dbo.Answers WITH CHECK
        ADD CONSTRAINT FK_Answers_Questions
            FOREIGN KEY (QuestionId) REFERENCES dbo.Questions(Id) ON DELETE CASCADE;');
    END;

    ---------------------------------------------------------------------------
    -- 6. TestHistories
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.TestHistories', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.TestHistories (
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            UserId NVARCHAR(450) NOT NULL,
            DeckId INT NOT NULL,
            Score FLOAT NOT NULL,
            Percentage FLOAT NOT NULL,
            CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_TestHistories_CreatedAt DEFAULT (GETUTCDATE())
        );

        PRINT N'Created TestHistories table';
    END
    ELSE
    BEGIN
        IF COL_LENGTH(N'dbo.TestHistories', N'UserId') IS NULL
            ALTER TABLE dbo.TestHistories ADD UserId NVARCHAR(450) NOT NULL CONSTRAINT DF_TestHistories_UserId DEFAULT (N'');

        IF COL_LENGTH(N'dbo.TestHistories', N'DeckId') IS NULL
            ALTER TABLE dbo.TestHistories ADD DeckId INT NOT NULL CONSTRAINT DF_TestHistories_DeckId DEFAULT (0);

        IF COL_LENGTH(N'dbo.TestHistories', N'Score') IS NULL
            ALTER TABLE dbo.TestHistories ADD Score FLOAT NOT NULL CONSTRAINT DF_TestHistories_Score DEFAULT (0);

        IF COL_LENGTH(N'dbo.TestHistories', N'Percentage') IS NULL
            ALTER TABLE dbo.TestHistories ADD Percentage FLOAT NOT NULL CONSTRAINT DF_TestHistories_Percentage DEFAULT (0);

        IF COL_LENGTH(N'dbo.TestHistories', N'CreatedAt') IS NULL
            ALTER TABLE dbo.TestHistories ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_TestHistories_CreatedAt DEFAULT (GETUTCDATE());
    END;

    IF COL_LENGTH(N'dbo.TestHistories', N'CreatedAt') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.TestHistories')
              AND c.name = N'CreatedAt'
       )
    BEGIN
        EXEC(N'UPDATE dbo.TestHistories SET CreatedAt = GETUTCDATE() WHERE CreatedAt IS NULL;');
        EXEC(N'ALTER TABLE dbo.TestHistories ADD CONSTRAINT DF_TestHistories_CreatedAt DEFAULT (GETUTCDATE()) FOR CreatedAt;');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_TestHistories_Users'
          AND parent_object_id = OBJECT_ID(N'dbo.TestHistories')
    )
    BEGIN
        EXEC(N'ALTER TABLE dbo.TestHistories WITH CHECK
        ADD CONSTRAINT FK_TestHistories_Users
            FOREIGN KEY (UserId) REFERENCES dbo.Users(Id);');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_TestHistories_Decks'
          AND parent_object_id = OBJECT_ID(N'dbo.TestHistories')
    )
    BEGIN
        EXEC(N'ALTER TABLE dbo.TestHistories WITH CHECK
        ADD CONSTRAINT FK_TestHistories_Decks
            FOREIGN KEY (DeckId) REFERENCES dbo.Decks(Id);');
    END;

    ---------------------------------------------------------------------------
    -- 7. TestResultDetails
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.TestResultDetails', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.TestResultDetails (
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            TestHistoryId INT NOT NULL,
            QuestionId INT NOT NULL,
            SelectedAnswerId INT NULL,
            IsCorrect BIT NOT NULL
        );

        PRINT N'Created TestResultDetails table';
    END
    ELSE
    BEGIN
        IF COL_LENGTH(N'dbo.TestResultDetails', N'TestHistoryId') IS NULL
            ALTER TABLE dbo.TestResultDetails ADD TestHistoryId INT NOT NULL CONSTRAINT DF_TestResultDetails_TestHistoryId DEFAULT (0);

        IF COL_LENGTH(N'dbo.TestResultDetails', N'QuestionId') IS NULL
            ALTER TABLE dbo.TestResultDetails ADD QuestionId INT NOT NULL CONSTRAINT DF_TestResultDetails_QuestionId DEFAULT (0);

        IF COL_LENGTH(N'dbo.TestResultDetails', N'SelectedAnswerId') IS NULL
            ALTER TABLE dbo.TestResultDetails ADD SelectedAnswerId INT NULL;

        IF COL_LENGTH(N'dbo.TestResultDetails', N'IsCorrect') IS NULL
            ALTER TABLE dbo.TestResultDetails ADD IsCorrect BIT NOT NULL CONSTRAINT DF_TestResultDetails_IsCorrect DEFAULT (0);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_Details_History'
          AND parent_object_id = OBJECT_ID(N'dbo.TestResultDetails')
    )
    BEGIN
        EXEC(N'ALTER TABLE dbo.TestResultDetails WITH CHECK
        ADD CONSTRAINT FK_Details_History
            FOREIGN KEY (TestHistoryId) REFERENCES dbo.TestHistories(Id) ON DELETE CASCADE;');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_Details_Question'
          AND parent_object_id = OBJECT_ID(N'dbo.TestResultDetails')
    )
    BEGIN
        EXEC(N'ALTER TABLE dbo.TestResultDetails WITH CHECK
        ADD CONSTRAINT FK_Details_Question
            FOREIGN KEY (QuestionId) REFERENCES dbo.Questions(Id);');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_Details_Answer'
          AND parent_object_id = OBJECT_ID(N'dbo.TestResultDetails')
    )
    BEGIN
        EXEC(N'ALTER TABLE dbo.TestResultDetails WITH CHECK
        ADD CONSTRAINT FK_Details_Answer
            FOREIGN KEY (SelectedAnswerId) REFERENCES dbo.Answers(Id);');
    END;

    ---------------------------------------------------------------------------
    -- 8. QuestionReports
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.QuestionReports', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.QuestionReports (
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            QuestionId INT NOT NULL,
            UserId NVARCHAR(450) NOT NULL,
            Reason NVARCHAR(100) NOT NULL,
            Note NVARCHAR(500) NULL,
            IsResolved BIT NOT NULL CONSTRAINT DF_QuestionReports_IsResolved DEFAULT (0),
            CreatedAt DATETIME NOT NULL CONSTRAINT DF_QuestionReports_CreatedAt DEFAULT (GETUTCDATE())
        );

        PRINT N'Created QuestionReports table';
    END
    ELSE
    BEGIN
        IF COL_LENGTH(N'dbo.QuestionReports', N'QuestionId') IS NULL
            ALTER TABLE dbo.QuestionReports ADD QuestionId INT NOT NULL CONSTRAINT DF_QuestionReports_QuestionId DEFAULT (0);

        IF COL_LENGTH(N'dbo.QuestionReports', N'UserId') IS NULL
            ALTER TABLE dbo.QuestionReports ADD UserId NVARCHAR(450) NOT NULL CONSTRAINT DF_QuestionReports_UserId DEFAULT (N'');

        IF COL_LENGTH(N'dbo.QuestionReports', N'Reason') IS NULL
            ALTER TABLE dbo.QuestionReports ADD Reason NVARCHAR(100) NOT NULL CONSTRAINT DF_QuestionReports_Reason DEFAULT (N'Other');

        IF COL_LENGTH(N'dbo.QuestionReports', N'Note') IS NULL
            ALTER TABLE dbo.QuestionReports ADD Note NVARCHAR(500) NULL;

        IF COL_LENGTH(N'dbo.QuestionReports', N'IsResolved') IS NULL
            ALTER TABLE dbo.QuestionReports ADD IsResolved BIT NOT NULL CONSTRAINT DF_QuestionReports_IsResolved DEFAULT (0);

        IF COL_LENGTH(N'dbo.QuestionReports', N'CreatedAt') IS NULL
            ALTER TABLE dbo.QuestionReports ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_QuestionReports_CreatedAt DEFAULT (GETUTCDATE());
    END;

    IF COL_LENGTH(N'dbo.QuestionReports', N'IsResolved') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.QuestionReports')
              AND c.name = N'IsResolved'
       )
    BEGIN
        EXEC(N'UPDATE dbo.QuestionReports SET IsResolved = 0 WHERE IsResolved IS NULL;');
        EXEC(N'ALTER TABLE dbo.QuestionReports ADD CONSTRAINT DF_QuestionReports_IsResolved DEFAULT (0) FOR IsResolved;');
    END;

    IF COL_LENGTH(N'dbo.QuestionReports', N'CreatedAt') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.QuestionReports')
              AND c.name = N'CreatedAt'
       )
    BEGIN
        EXEC(N'UPDATE dbo.QuestionReports SET CreatedAt = GETUTCDATE() WHERE CreatedAt IS NULL;');
        EXEC(N'ALTER TABLE dbo.QuestionReports ADD CONSTRAINT DF_QuestionReports_CreatedAt DEFAULT (GETUTCDATE()) FOR CreatedAt;');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_QuestionReports_Questions'
          AND parent_object_id = OBJECT_ID(N'dbo.QuestionReports')
    )
    BEGIN
        EXEC(N'ALTER TABLE dbo.QuestionReports WITH CHECK
        ADD CONSTRAINT FK_QuestionReports_Questions
            FOREIGN KEY (QuestionId) REFERENCES dbo.Questions(Id);');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_QuestionReports_Users'
          AND parent_object_id = OBJECT_ID(N'dbo.QuestionReports')
    )
    BEGIN
        EXEC(N'ALTER TABLE dbo.QuestionReports WITH CHECK
        ADD CONSTRAINT FK_QuestionReports_Users
            FOREIGN KEY (UserId) REFERENCES dbo.Users(Id);');
    END;

    ---------------------------------------------------------------------------
    -- 9. LoginAttempts
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.LoginAttempts', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.LoginAttempts (
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            Email NVARCHAR(256) NOT NULL,
            IpAddress NVARCHAR(50) NOT NULL,
            IsSuccess BIT NOT NULL,
            UserId NVARCHAR(450) NULL,
            CreatedAt DATETIME NOT NULL CONSTRAINT DF_LoginAttempts_CreatedAt DEFAULT (GETUTCDATE())
        );

        PRINT N'Created LoginAttempts table';
    END
    ELSE
    BEGIN
        IF COL_LENGTH(N'dbo.LoginAttempts', N'Email') IS NULL
            ALTER TABLE dbo.LoginAttempts ADD Email NVARCHAR(256) NOT NULL CONSTRAINT DF_LoginAttempts_Email DEFAULT (N'');

        IF COL_LENGTH(N'dbo.LoginAttempts', N'IpAddress') IS NULL
            ALTER TABLE dbo.LoginAttempts ADD IpAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_LoginAttempts_IpAddress DEFAULT (N'');

        IF COL_LENGTH(N'dbo.LoginAttempts', N'IsSuccess') IS NULL
            ALTER TABLE dbo.LoginAttempts ADD IsSuccess BIT NOT NULL CONSTRAINT DF_LoginAttempts_IsSuccess DEFAULT (0);

        IF COL_LENGTH(N'dbo.LoginAttempts', N'UserId') IS NULL
            ALTER TABLE dbo.LoginAttempts ADD UserId NVARCHAR(450) NULL;

        IF COL_LENGTH(N'dbo.LoginAttempts', N'CreatedAt') IS NULL
            ALTER TABLE dbo.LoginAttempts ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_LoginAttempts_CreatedAt DEFAULT (GETUTCDATE());
    END;

    IF COL_LENGTH(N'dbo.LoginAttempts', N'CreatedAt') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.LoginAttempts')
              AND c.name = N'CreatedAt'
       )
    BEGIN
        EXEC(N'UPDATE dbo.LoginAttempts SET CreatedAt = GETUTCDATE() WHERE CreatedAt IS NULL;');
        EXEC(N'ALTER TABLE dbo.LoginAttempts ADD CONSTRAINT DF_LoginAttempts_CreatedAt DEFAULT (GETUTCDATE()) FOR CreatedAt;');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_LoginAttempts_Users'
          AND parent_object_id = OBJECT_ID(N'dbo.LoginAttempts')
    )
    BEGIN
        EXEC(N'ALTER TABLE dbo.LoginAttempts WITH CHECK
        ADD CONSTRAINT FK_LoginAttempts_Users
            FOREIGN KEY (UserId) REFERENCES dbo.Users(Id);');
    END;

    COMMIT TRANSACTION;

    PRINT N'CreateDB.sql completed successfully. Database schema is up to date.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT N'CreateDB.sql failed. No partial schema changes were committed.';
    THROW;
END CATCH;
GO
