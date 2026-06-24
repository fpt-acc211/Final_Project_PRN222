using BusinessObjects;
using Microsoft.AspNetCore.Mvc;
using QuizManagement.Controllers;
using QuizManagement.Tests.TestHelpers;
using QuizManagement.ViewModels.QuestionReports;
using Services;

namespace QuizManagement.Tests.Controllers;

public class QuestionReportsControllerTests
{
    [Fact]
    public void CreatePost_WhenPendingReportExists_DoesNotSubmitAgain()
    {
        var reportService = new FakeQuestionReportService { HasPendingReportResult = true };
        var questionService = new FakeQuestionService
        {
            Question = CreateQuestion(ownerUserId: "mentor-1")
        };
        var controller = CreateController(reportService, questionService, "user-1", AppRoles.User);

        var result = controller.Create(new SubmitReportViewModel
        {
            QuestionId = 10,
            Reason = "WrongAnswer",
            TestHistoryId = 77
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Result", redirect.ActionName);
        Assert.Equal("Quiz", redirect.ControllerName);
        Assert.Equal(0, reportService.SubmitCount);
        Assert.Equal((10, "user-1"), reportService.LastPendingReportQuery);
    }

    [Fact]
    public void CreatePost_WithInvalidReason_ReturnsViewAndDoesNotSubmit()
    {
        var reportService = new FakeQuestionReportService();
        var questionService = new FakeQuestionService
        {
            Question = CreateQuestion(ownerUserId: "mentor-1")
        };
        var controller = CreateController(reportService, questionService, "user-1", AppRoles.User);

        var result = controller.Create(new SubmitReportViewModel
        {
            QuestionId = 10,
            Reason = "TamperedReason"
        });

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<SubmitReportViewModel>(view.Model);
        Assert.Equal("Question content", model.QuestionContent);
        Assert.Equal(0, reportService.SubmitCount);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public void Resolve_WhenMentorDoesNotOwnReport_ReturnsForbid()
    {
        var reportService = new FakeQuestionReportService
        {
            Report = CreateReport(ownerUserId: "mentor-2")
        };
        var controller = CreateController(reportService, new FakeQuestionService(), "mentor-1", AppRoles.Mentor);

        var result = controller.Resolve(123);

        Assert.IsType<ForbidResult>(result);
        Assert.Null(reportService.ResolvedReportId);
    }

    [Fact]
    public void Resolve_WhenMentorOwnsReport_ResolvesAndRedirects()
    {
        var reportService = new FakeQuestionReportService
        {
            Report = CreateReport(ownerUserId: "mentor-1")
        };
        var controller = CreateController(reportService, new FakeQuestionService(), "mentor-1", AppRoles.Mentor);

        var result = controller.Resolve(123);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(123, reportService.ResolvedReportId);
    }

    [Fact]
    public void Resolve_WhenUserIsAdmin_ResolvesAnyReport()
    {
        var reportService = new FakeQuestionReportService
        {
            Report = CreateReport(ownerUserId: "mentor-2")
        };
        var controller = CreateController(reportService, new FakeQuestionService(), "admin-1", AppRoles.Admin);

        var result = controller.Resolve(123);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(123, reportService.ResolvedReportId);
    }

    private static QuestionReportsController CreateController(
        FakeQuestionReportService reportService,
        FakeQuestionService questionService,
        string userId,
        params string[] roles)
    {
        var controller = new QuestionReportsController(reportService, questionService);
        ControllerTestHelper.ConfigureUser(controller, userId, roles);
        return controller;
    }

    private static Question CreateQuestion(string ownerUserId)
    {
        return new Question
        {
            Id = 10,
            Content = "Question content",
            Deck = new Deck
            {
                Id = 20,
                Name = "Deck",
                Subject = new Subject
                {
                    Id = 30,
                    Name = "Subject",
                    UserId = ownerUserId
                }
            }
        };
    }

    private static QuestionReport CreateReport(string ownerUserId)
    {
        return new QuestionReport
        {
            Id = 123,
            QuestionId = 10,
            UserId = "user-1",
            Reason = "WrongAnswer",
            Question = CreateQuestion(ownerUserId)
        };
    }

    private sealed class FakeQuestionReportService : IQuestionReportService
    {
        public QuestionReport? Report { get; set; }
        public bool HasPendingReportResult { get; set; }
        public int SubmitCount { get; private set; }
        public int? ResolvedReportId { get; private set; }
        public (int QuestionId, string UserId)? LastPendingReportQuery { get; private set; }

        public void Submit(int questionId, string userId, string reason, string? note)
        {
            SubmitCount++;
        }

        public List<QuestionReport> GetAllReports() => new();
        public List<QuestionReport> GetReportsByContentOwner(string ownerUserId) => new();
        public QuestionReport? GetReportById(int reportId) => Report;

        public void Resolve(int reportId)
        {
            ResolvedReportId = reportId;
        }

        public bool HasPendingReport(int questionId, string userId)
        {
            LastPendingReportQuery = (questionId, userId);
            return HasPendingReportResult;
        }
    }

    private sealed class FakeQuestionService : IQuestionService
    {
        public Question? Question { get; set; }

        public IEnumerable<Question> GetQuestionsByDeckForStudy(int deckId) => new List<Question>();
        public IEnumerable<Question> GetQuestionsByDeck(int deckId, string userId) => new List<Question>();
        public Question? GetQuestionById(int id, string userId, bool allowAll = false) => Question;
        public void AddQuestion(Question question) { }
        public void UpdateQuestion(Question question) { }
        public void DeleteQuestion(Question question) { }
    }
}
