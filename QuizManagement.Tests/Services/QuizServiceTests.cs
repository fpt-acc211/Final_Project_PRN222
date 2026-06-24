using BusinessObjects;
using Repositories;
using Services;

namespace QuizManagement.Tests.Services;

public class QuizServiceTests
{
    [Fact]
    public void GradeAndSaveQuiz_WhenAnswersAreCorrect_ComputesFullScore()
    {
        var repository = new FakeQuizRepository(CreateQuestions());
        var service = new QuizService(repository);

        var history = service.GradeAndSaveQuiz(
            deckId: 1,
            userId: "user-1",
            questionIds: new[] { 101, 102 },
            selectedAnswerIdsByQuestion: new Dictionary<int, List<int>>
            {
                [101] = new() { 1001 },
                [102] = new() { 2001, 2002 }
            });

        Assert.Equal(10, history.Score);
        Assert.Equal(100, history.Percentage);
        Assert.Equal(3, history.TestResultDetails.Count);
        Assert.All(history.TestResultDetails, detail => Assert.True(detail.IsCorrect));
        Assert.Same(history, repository.SavedHistory);
    }

    [Fact]
    public void GradeAndSaveQuiz_WhenInvalidAnswerIdIsPosted_IgnoresIt()
    {
        var repository = new FakeQuizRepository(CreateQuestions());
        var service = new QuizService(repository);

        var history = service.GradeAndSaveQuiz(
            deckId: 1,
            userId: "user-1",
            questionIds: new[] { 101 },
            selectedAnswerIdsByQuestion: new Dictionary<int, List<int>>
            {
                [101] = new() { 9999 }
            });

        Assert.Equal(0, history.Score);
        var detail = Assert.Single(history.TestResultDetails);
        Assert.Null(detail.SelectedAnswerId);
        Assert.False(detail.IsCorrect);
    }

    [Fact]
    public void GradeAndSaveQuiz_WithDuplicateQuestionIds_GradesQuestionOnce()
    {
        var repository = new FakeQuizRepository(CreateQuestions());
        var service = new QuizService(repository);

        var history = service.GradeAndSaveQuiz(
            deckId: 1,
            userId: "user-1",
            questionIds: new[] { 101, 101 },
            selectedAnswerIdsByQuestion: new Dictionary<int, List<int>>
            {
                [101] = new() { 1001 }
            });

        Assert.Equal(10, history.Score);
        Assert.Single(history.TestResultDetails);
    }

    private static List<Question> CreateQuestions()
    {
        return new List<Question>
        {
            new()
            {
                Id = 101,
                DeckId = 1,
                Content = "Single choice",
                QuestionType = 1,
                Answers = new List<Answer>
                {
                    new() { Id = 1001, QuestionId = 101, Content = "Correct", IsCorrect = true },
                    new() { Id = 1002, QuestionId = 101, Content = "Wrong", IsCorrect = false }
                }
            },
            new()
            {
                Id = 102,
                DeckId = 1,
                Content = "Multiple choice",
                QuestionType = 2,
                Answers = new List<Answer>
                {
                    new() { Id = 2001, QuestionId = 102, Content = "Correct A", IsCorrect = true },
                    new() { Id = 2002, QuestionId = 102, Content = "Correct B", IsCorrect = true },
                    new() { Id = 2003, QuestionId = 102, Content = "Wrong", IsCorrect = false }
                }
            }
        };
    }

    private sealed class FakeQuizRepository : IQuizRepository
    {
        private readonly List<Question> _questions;

        public FakeQuizRepository(List<Question> questions)
        {
            _questions = questions;
        }

        public TestHistory? SavedHistory { get; private set; }

        public List<Question> GetQuestionsForQuiz(int deckId)
            => _questions.Where(q => q.DeckId == deckId).ToList();

        public int GetQuestionCount(int deckId)
            => _questions.Count(q => q.DeckId == deckId);

        public TestHistory SaveTestResult(TestHistory history)
        {
            history.Id = 500;
            SavedHistory = history;
            return history;
        }

        public TestHistory? GetTestHistoryById(int id, string userId) => null;
        public List<TestHistory> GetTestHistoriesByUser(string userId) => new();
        public List<TestHistory> GetRecentTestHistories(string userId, int count) => new();
        public (int totalQuizzes, double averagePercentage, DateTime? lastQuizDate) GetQuizStatistics(string userId) => (0, 0, null);
        public List<TestHistory> GetTestHistoriesByDeck(int deckId) => new();
        public List<TestHistory> GetTestHistoriesByContentOwner(string ownerUserId, bool isAdmin) => new();
    }
}
