using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace QuizManagement.Tests.IntegrationTests;

public class DemoSeedSqlServerTests
{
    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public void UpgradeScript_PreservesLegacyUsersAndIsIdempotent()
    {
        var databaseName = $"QuizManagementTests_{Guid.NewGuid():N}";
        using var master = new SqlConnection(SqlServerTestConnection.ForDatabase("master"));
        master.Open();
        new SqlCommand($"CREATE DATABASE [{databaseName}]", master).ExecuteNonQuery();

        try
        {
            using var database = new SqlConnection(SqlServerTestConnection.ForDatabase(databaseName));
            database.Open();
            new SqlCommand("""
                CREATE TABLE dbo.Users (Id NVARCHAR(450) PRIMARY KEY, Email NVARCHAR(256) NOT NULL);
                CREATE TABLE dbo.Questions (Id INT PRIMARY KEY);
                CREATE TABLE dbo.LoginAttempts (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Email NVARCHAR(256) NOT NULL,
                    IpAddress NVARCHAR(50) NOT NULL,
                    IsSuccess BIT NOT NULL,
                    UserId NVARCHAR(450) NULL,
                    CreatedAt DATETIME2(7) NOT NULL,
                    CONSTRAINT FK_LegacyLoginAttempts_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id));
                INSERT INTO dbo.Users (Id, Email) VALUES (N'legacy', N'legacy@test.local');
                INSERT INTO dbo.Questions (Id) VALUES (1);
                """, database).ExecuteNonQuery();

            ExecuteUpgrade(database, databaseName);
            ExecuteUpgrade(database, databaseName);

            Assert.Equal(1, Scalar(database, "SELECT CONVERT(INT, EmailConfirmed) FROM dbo.Users WHERE Id = N'legacy'"));
            Assert.Equal(1, Scalar(database, "SELECT COUNT(*) FROM sys.tables WHERE name = N'FlashcardProgresses'"));
            new SqlCommand("INSERT INTO dbo.Users (Id, Email) VALUES (N'new', N'new@test.local')", database)
                .ExecuteNonQuery();
            Assert.Equal(0, Scalar(database, "SELECT CONVERT(INT, EmailConfirmed) FROM dbo.Users WHERE Id = N'new'"));
        }
        finally
        {
            new SqlCommand(
                $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]",
                master).ExecuteNonQuery();
        }
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public void CreateScript_RejectsPartialSchemaWithoutMutation()
    {
        var databaseName = $"QuizManagementTests_{Guid.NewGuid():N}";
        var masterConnectionString = SqlServerTestConnection.ForDatabase("master");
        var databaseConnectionString = SqlServerTestConnection.ForDatabase(databaseName);
        using var master = new SqlConnection(masterConnectionString);
        master.Open();
        new SqlCommand($"CREATE DATABASE [{databaseName}]", master).ExecuteNonQuery();

        try
        {
            using var database = new SqlConnection(databaseConnectionString);
            database.Open();
            new SqlCommand("CREATE TABLE dbo.Users (Id INT NOT NULL PRIMARY KEY)", database).ExecuteNonQuery();

            var exception = Assert.Throws<SqlException>(() => ExecuteSchema(database, databaseName));

            Assert.Equal(51020, exception.Number);
            Assert.Equal(1, Scalar(database, "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0"));
        }
        finally
        {
            new SqlCommand(
                $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]",
                master).ExecuteNonQuery();
        }
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public void Seed_RequiresOptInAndRerunsAfterDemoActivityWithoutDeletingUnrelatedData()
    {
        var databaseName = $"QuizManagementTests_{Guid.NewGuid():N}";
        var masterConnectionString = SqlServerTestConnection.ForDatabase("master");
        var databaseConnectionString = SqlServerTestConnection.ForDatabase(databaseName);
        using var master = new SqlConnection(masterConnectionString);
        master.Open();

        try
        {
            ExecuteSchema(master, databaseName);

            using var database = new SqlConnection(databaseConnectionString);
            database.Open();
            ExecuteSchema(database, databaseName);

            var blocked = Assert.Throws<SqlException>(() => ExecuteSeed(database, databaseName));
            Assert.Equal(51019, blocked.Number);
            Assert.Equal(0, Scalar(database, "SELECT COUNT(*) FROM dbo.Users"));

            EnableSeed(database);
            ExecuteSeed(database, databaseName);
            AddDemoActivityAndUnrelatedData(database);

            EnableSeed(database);
            ExecuteSeed(database, databaseName);

            Assert.Equal(3, Scalar(database, "SELECT COUNT(*) FROM dbo.Users WHERE Id LIKE N'seed-%'"));
            Assert.Equal(1, Scalar(database, "SELECT COUNT(*) FROM dbo.Users WHERE Id = N'outside-user'"));
            Assert.Equal(2, Scalar(database, "SELECT COUNT(*) FROM dbo.QuestionReports WHERE UserId = N'seed-user-001'"));
            Assert.Equal(1, Scalar(database, "SELECT COUNT(*) FROM dbo.QuestionReports WHERE UserId = N'seed-user-001' AND IsResolved = 0"));
            Assert.Equal(1, Scalar(database, "SELECT COUNT(*) FROM dbo.QuestionReports WHERE UserId = N'seed-user-001' AND IsResolved = 1"));
            Assert.Equal(1, Scalar(database, "SELECT COUNT(*) FROM dbo.QuizAttempts WHERE UserId = N'seed-user-001' AND CompletedAtUtc IS NOT NULL"));
            Assert.Equal(0, Scalar(database, "SELECT COUNT(*) FROM dbo.QuizAttempts WHERE UserId = N'seed-user-001' AND CompletedAtUtc IS NULL"));
            Assert.Equal(1, Scalar(database, """
                SELECT COUNT(*)
                FROM dbo.TestHistories
                WHERE UserId = N'seed-user-001'
                  AND QuizAttemptId IS NOT NULL
                  AND ISJSON(ResultSnapshotJson) = 1
                  AND JSON_VALUE(ResultSnapshotJson, '$.DeckName') = N'ASP.NET Core MVC'
                """));
            Assert.Equal(5, Scalar(database, """
                SELECT COUNT(*)
                FROM dbo.TestHistories history
                CROSS APPLY OPENJSON(history.ResultSnapshotJson, '$.Questions') questions
                WHERE history.UserId = N'seed-user-001'
                """));
            Assert.Equal(7, Scalar(database, """
                SELECT COUNT(*)
                FROM dbo.TestResultDetails detail
                JOIN dbo.TestHistories history ON history.Id = detail.TestHistoryId
                WHERE history.UserId = N'seed-user-001'
                """));
            Assert.Equal(2, Scalar(database, "SELECT COUNT(*) FROM dbo.LoginAttempts WHERE Email = N'user.demo@quiz.local'"));
            Assert.Equal(1, Scalar(database, "SELECT COUNT(*) FROM dbo.LoginAttempts WHERE Email = N'unknown.demo@quiz.local'"));
            Assert.Equal(1, Scalar(database, "SELECT COUNT(*) FROM dbo.LoginAttempts WHERE Email = N'outside@test.local'"));
            Assert.Equal(0, Scalar(database, """
                SELECT ISNULL(TRY_CONVERT(INT, SESSION_CONTEXT(N'QuizManagement.AllowDemoSeed')), 0)
                """));
        }
        finally
        {
            new SqlCommand(
                $"USE [master]; ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]",
                master).ExecuteNonQuery();
        }
    }

    private static void AddDemoActivityAndUnrelatedData(SqlConnection database)
    {
        new SqlCommand("""
            INSERT INTO dbo.Users
                (Id, Username, Email, PasswordHash, Role, IsDisabled, SecurityStamp, CreatedAt)
            VALUES
                (N'outside-user', N'outside-user', N'outside@test.local', NULL, N'User', 0, N'stamp', SYSUTCDATETIME());

            DECLARE @QuestionId INT = (
                SELECT TOP (1) q.Id
                FROM dbo.Questions q
                JOIN dbo.Decks d ON d.Id = q.DeckId
                JOIN dbo.Subjects s ON s.Id = d.SubjectId
                WHERE s.UserId = N'seed-mentor-001');
            DECLARE @DeckId INT = (
                SELECT TOP (1) d.Id
                FROM dbo.Decks d
                JOIN dbo.Subjects s ON s.Id = d.SubjectId
                WHERE s.UserId = N'seed-mentor-001');

            INSERT INTO dbo.QuestionReports (QuestionId, UserId, Reason, IsResolved, CreatedAt)
            VALUES
                (@QuestionId, N'seed-user-001', N'WrongAnswer', 0, SYSUTCDATETIME()),
                (@QuestionId, N'outside-user', N'Other', 0, SYSUTCDATETIME());

            INSERT INTO dbo.LoginAttempts (Email, IpAddress, IsSuccess, UserId, CreatedAt)
            VALUES
                (N'user.demo@quiz.local', N'127.0.0.1', 1, N'seed-user-001', SYSUTCDATETIME()),
                (N'outside@test.local', N'127.0.0.2', 1, N'outside-user', SYSUTCDATETIME());

            INSERT INTO dbo.QuizAttempts
                (Id, UserId, DeckId, QuestionIdsJson, TimeLimitMinutes, StartedAtUtc)
            VALUES
                (NEWID(), N'seed-user-001', @DeckId, N'[]', 0, SYSDATETIMEOFFSET());
            """, database).ExecuteNonQuery();
    }

    private static void ExecuteSchema(SqlConnection database, string databaseName)
    {
        var path = RootFile("CreateDB.sql");
        var script = File.ReadAllText(path)
            .Replace("QuizManagementDB", databaseName, StringComparison.Ordinal);
        ExecuteBatches(database, script);
    }

    private static void ExecuteSeed(SqlConnection database, string databaseName)
    {
        var path = RootFile("SeedDemoData.sql");
        var script = File.ReadAllText(path)
            .Replace("USE QuizManagementDB;", $"USE [{databaseName}];", StringComparison.Ordinal);
        ExecuteBatches(database, script);
    }

    private static void ExecuteUpgrade(SqlConnection database, string databaseName)
    {
        var script = File.ReadAllText(RootFile("UpgradeDB_20260722.sql"))
            .Replace("QuizManagementDB", databaseName, StringComparison.Ordinal);
        ExecuteBatches(database, script);
    }

    private static void EnableSeed(SqlConnection database)
        => new SqlCommand("""
            EXEC sys.sp_set_session_context
                @key = N'QuizManagement.AllowDemoSeed',
                @value = 1;
            """, database).ExecuteNonQuery();

    private static void ExecuteBatches(SqlConnection database, string script)
    {
        foreach (var batch in Regex.Split(
                     script,
                     @"^\s*GO\s*(?:--.*)?$",
                     RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(batch))
                new SqlCommand(batch, database) { CommandTimeout = 60 }.ExecuteNonQuery();
        }
    }

    private static string RootFile(string fileName)
        => Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            fileName));

    private static int Scalar(SqlConnection connection, string sql)
        => Convert.ToInt32(new SqlCommand(sql, connection).ExecuteScalar());
}
