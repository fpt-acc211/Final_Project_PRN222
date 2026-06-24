using System.Text.Json;
using BusinessObjects;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using QuizManagement.Controllers;
using QuizManagement.Tests.TestHelpers;
using QuizManagement.ViewModels.Quiz;
using Services;

namespace QuizManagement.Tests.Controllers;

public class QuizControllerTests
{
    [Fact]
    public void Submit_WhenAttemptTokenIsExpired_RedirectsToConfigWithoutGrading()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var quizService = new FakeQuizService();
        var controller = CreateController(dataProtectionProvider, quizService, "user-1");
        var token = CreateAttemptToken(
            dataProtectionProvider,
            deckId: 1,
            userId: "user-1",
            issuedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-10),
            expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = controller.Submit(new QuizSubmitViewModel
        {
            DeckId = 1,
            AttemptToken = token
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Config", redirect.ActionName);
        Assert.False(quizService.GradeCalled);
        Assert.True(controller.TempData.ContainsKey("ErrorMessage"));
    }

    [Fact]
    public void Submit_WhenAttemptTokenIsValid_GradesAndRedirectsToResult()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var quizService = new FakeQuizService
        {
            HistoryToReturn = new TestHistory { Id = 321, UserId = "user-1", DeckId = 1 }
        };
        var controller = CreateController(dataProtectionProvider, quizService, "user-1");
        var token = CreateAttemptToken(
            dataProtectionProvider,
            deckId: 1,
            userId: "user-1",
            issuedAtUtc: DateTimeOffset.UtcNow,
            expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(5),
            questionIds: new[] { 101, 102 });

        var result = controller.Submit(new QuizSubmitViewModel
        {
            DeckId = 1,
            AttemptToken = token,
            Questions =
            {
                new QuizQuestionSubmitItem { QuestionId = 101, SelectedAnswerId = 1001 },
                new QuizQuestionSubmitItem { QuestionId = 102, SelectedAnswerIds = new List<int> { 2001, 2002 } }
            }
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Result", redirect.ActionName);
        Assert.Equal(321, redirect.RouteValues?["id"]);
        Assert.True(quizService.GradeCalled);
        Assert.Equal(new[] { 101, 102 }, quizService.LastQuestionIds);
        Assert.Equal(new[] { 1001 }, quizService.LastSelectedAnswersByQuestion![101]);
        Assert.Equal(new[] { 2001, 2002 }, quizService.LastSelectedAnswersByQuestion![102]);
    }

    [Fact]
    public void Submit_WhenTokenBelongsToAnotherUser_RedirectsToConfig()
    {
        var dataProtectionProvider = CreateDataProtectionProvider();
        var quizService = new FakeQuizService();
        var controller = CreateController(dataProtectionProvider, quizService, "user-1");
        var token = CreateAttemptToken(
            dataProtectionProvider,
            deckId: 1,
            userId: "user-2",
            issuedAtUtc: DateTimeOffset.UtcNow,
            expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(5));

        var result = controller.Submit(new QuizSubmitViewModel
        {
            DeckId = 1,
            AttemptToken = token
        });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.False(quizService.GradeCalled);
    }

    private static QuizController CreateController(
        IDataProtectionProvider dataProtectionProvider,
        FakeQuizService quizService,
        string userId)
    {
        var controller = new QuizController(
            new FakeDeckService(),
            quizService,
            dataProtectionProvider);

        ControllerTestHelper.ConfigureUser(controller, userId, AppRoles.User);
        return controller;
    }

    private static IDataProtectionProvider CreateDataProtectionProvider()
    {
        var directory = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "QuizManagement.Tests", Guid.NewGuid().ToString("N")));

        return DataProtectionProvider.Create(directory);
    }

    private static string CreateAttemptToken(
        IDataProtectionProvider dataProtectionProvider,
        int deckId,
        string userId,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset? expiresAtUtc,
        IReadOnlyList<int>? questionIds = null)
    {
        var payload = new
        {
            DeckId = deckId,
            UserId = userId,
            QuestionIds = questionIds ?? new[] { 101 },
            IssuedAtUtc = issuedAtUtc,
            ExpiresAtUtc = expiresAtUtc
        };

        return dataProtectionProvider
            .CreateProtector("QuizManagement.QuizAttempt.v1")
            .Protect(JsonSerializer.Serialize(payload));
    }

    private sealed class FakeDeckService : IDeckService
    {
        public IEnumerable<Deck> GetDecksBySubjectForStudy(int subjectId) => new List<Deck>();
        public IEnumerable<Deck> GetDecksBySubject(int subjectId, string userId) => new List<Deck>();

        public Deck? GetDeckForStudy(int id)
            => new()
            {
                Id = id,
                Name = "Deck",
                Subject = new Subject { Id = 1, Name = "Subject", UserId = "mentor-1" }
            };

        public Deck? GetDeckById(int id, string userId, bool allowAll = false) => GetDeckForStudy(id);
        public bool NameExists(int subjectId, string name, int? excludedId = null) => false;
        public void AddDeck(Deck deck) { }
        public void UpdateDeck(Deck deck) { }
        public void DeleteDeck(Deck deck) { }
    }

    private sealed class FakeQuizService : IQuizService
    {
        public bool GradeCalled { get; private set; }
        public IReadOnlyList<int>? LastQuestionIds { get; private set; }
        public IReadOnlyDictionary<int, List<int>>? LastSelectedAnswersByQuestion { get; private set; }
        public TestHistory HistoryToReturn { get; set; } = new() { Id = 1, UserId = "user-1", DeckId = 1 };

        public List<Question> GetQuestionsForQuiz(int deckId, int questionCount) => new();
        public int GetAvailableQuestionCount(int deckId) => 0;

        public TestHistory GradeAndSaveQuiz(
            int deckId,
            string userId,
            IReadOnlyList<int> questionIds,
            IReadOnlyDictionary<int, List<int>> selectedAnswerIdsByQuestion)
        {
            GradeCalled = true;
            LastQuestionIds = questionIds.ToList();
            LastSelectedAnswersByQuestion = selectedAnswerIdsByQuestion.ToDictionary(
                pair => pair.Key,
                pair => pair.Value);
            return HistoryToReturn;
        }

        public TestHistory? GetTestHistoryById(int id, string userId) => null;
        public List<TestHistory> GetTestHistoriesByUser(string userId) => new();
        public List<TestHistory> GetRecentTestHistories(string userId, int count) => new();
        public (int totalQuizzes, double averagePercentage, DateTime? lastQuizDate) GetQuizStatistics(string userId) => (0, 0, null);
        public List<TestHistory> GetTestHistoriesByDeck(int deckId) => new();
        public List<TestHistory> GetTestHistoriesByContentOwner(string ownerUserId, bool isAdmin) => new();
    }
}
