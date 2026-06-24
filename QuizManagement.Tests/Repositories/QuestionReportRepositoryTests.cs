using BusinessObjects;
using Repositories;
using QuizManagement.Tests.TestHelpers;

namespace QuizManagement.Tests.Repositories;

public class QuestionReportRepositoryTests
{
    [Fact]
    public void GetByContentOwner_ReturnsReportsForOwnerIncludingSoftDeletedContent()
    {
        using var context = TestDbContextFactory.CreateContext();
        SeedReports(context);
        var repository = new QuestionReportRepository(context);

        var result = repository.GetByContentOwner("mentor-1");

        var report = Assert.Single(result);
        Assert.Equal(9001, report.Id);
        Assert.Equal("Soft-deleted owned question", report.Question.Content);
        Assert.Equal("Owned deck", report.Question.Deck.Name);
        Assert.Equal("Owned subject", report.Question.Deck.Subject.Name);
    }

    [Fact]
    public void GetAll_ReturnsAllReportsIncludingSoftDeletedContent()
    {
        using var context = TestDbContextFactory.CreateContext();
        SeedReports(context);
        var repository = new QuestionReportRepository(context);

        var result = repository.GetAll();

        Assert.Equal(new[] { 9002, 9001 }, result.Select(r => r.Id));
    }

    [Fact]
    public void Resolve_UpdatesReportEvenWhenQuestionIsSoftDeleted()
    {
        using var context = TestDbContextFactory.CreateContext();
        SeedReports(context);
        var repository = new QuestionReportRepository(context);

        repository.Resolve(9001);

        var report = context.QuestionReports.Single(r => r.Id == 9001);
        Assert.True(report.IsResolved);
    }

    private static void SeedReports(DataAccessObjects.QuizManagementDbContext context)
    {
        context.Users.AddRange(
            new User { Id = "mentor-1", Username = "mentor1", Email = "mentor1@test.local", Role = AppRoles.Mentor },
            new User { Id = "mentor-2", Username = "mentor2", Email = "mentor2@test.local", Role = AppRoles.Mentor },
            new User { Id = "user-1", Username = "user1", Email = "user1@test.local", Role = AppRoles.User });

        context.Subjects.AddRange(
            new Subject { Id = 1, UserId = "mentor-1", Name = "Owned subject", IsDeleted = true },
            new Subject { Id = 2, UserId = "mentor-2", Name = "Other subject" });

        context.Decks.AddRange(
            new Deck { Id = 11, SubjectId = 1, Name = "Owned deck", IsDeleted = true },
            new Deck { Id = 22, SubjectId = 2, Name = "Other deck" });

        context.Questions.AddRange(
            new Question
            {
                Id = 101,
                DeckId = 11,
                Content = "Soft-deleted owned question",
                QuestionType = 1,
                IsDeleted = true
            },
            new Question
            {
                Id = 202,
                DeckId = 22,
                Content = "Other question",
                QuestionType = 1
            });

        context.QuestionReports.AddRange(
            new QuestionReport
            {
                Id = 9001,
                QuestionId = 101,
                UserId = "user-1",
                Reason = "UnclearQuestion",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            },
            new QuestionReport
            {
                Id = 9002,
                QuestionId = 202,
                UserId = "user-1",
                Reason = "WrongAnswer",
                CreatedAt = DateTime.UtcNow
            });

        context.SaveChanges();
    }
}
