using BusinessObjects;
using Repositories;
using Services;

namespace QuizManagement.Tests.Services;

public class QuestionReportServiceTests
{
    [Fact]
    public void Submit_TrimsNoteAndCreatesPendingReport()
    {
        var repository = new FakeQuestionReportRepository();
        var service = new QuestionReportService(repository);

        service.Submit(10, "user-1", "WrongAnswer", "  Please review  ");

        var report = Assert.Single(repository.CreatedReports);
        Assert.Equal(10, report.QuestionId);
        Assert.Equal("user-1", report.UserId);
        Assert.Equal("WrongAnswer", report.Reason);
        Assert.Equal("Please review", report.Note);
        Assert.False(report.IsResolved);
        Assert.True(DateTime.UtcNow.Subtract(report.CreatedAt) < TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Submit_WithBlankNote_SavesNullNote()
    {
        var repository = new FakeQuestionReportRepository();
        var service = new QuestionReportService(repository);

        service.Submit(10, "user-1", "Other", "   ");

        var report = Assert.Single(repository.CreatedReports);
        Assert.Null(report.Note);
    }

    [Fact]
    public void HasPendingReport_DelegatesToRepository()
    {
        var repository = new FakeQuestionReportRepository { HasPendingReportResult = true };
        var service = new QuestionReportService(repository);

        var result = service.HasPendingReport(10, "user-1");

        Assert.True(result);
        Assert.Equal((10, "user-1"), repository.LastPendingReportQuery);
    }

    private sealed class FakeQuestionReportRepository : IQuestionReportRepository
    {
        public List<QuestionReport> CreatedReports { get; } = new();
        public bool HasPendingReportResult { get; set; }
        public (int QuestionId, string UserId)? LastPendingReportQuery { get; private set; }

        public void Create(QuestionReport report) => CreatedReports.Add(report);
        public List<QuestionReport> GetAll() => new();
        public List<QuestionReport> GetByContentOwner(string ownerUserId) => new();
        public QuestionReport? GetById(int id) => null;
        public QuestionReport? GetByIdWithDetails(int id) => null;
        public void Resolve(int id) { }

        public bool HasPendingReport(int questionId, string userId)
        {
            LastPendingReportQuery = (questionId, userId);
            return HasPendingReportResult;
        }
    }
}
