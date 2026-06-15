using BusinessObjects;

namespace Repositories
{
    public interface IQuizRepository
    {
        List<Question> GetQuestionsForQuiz(int deckId, string userId);
        int GetQuestionCount(int deckId, string userId);
        TestHistory SaveTestResult(TestHistory history);
        TestHistory? GetTestHistoryById(int id, string userId);
        List<TestHistory> GetTestHistoriesByUser(string userId);
        List<TestHistory> GetRecentTestHistories(string userId, int count);
        (int totalQuizzes, double averagePercentage, DateTime? lastQuizDate) GetQuizStatistics(string userId);
    }
}
