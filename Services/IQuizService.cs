using BusinessObjects;

namespace Services
{
    public interface IQuizService
    {
        List<Question> GetQuestionsForQuiz(int deckId, int questionCount);
        int GetAvailableQuestionCount(int deckId);
        TestHistory GradeAndSaveQuiz(
            int deckId,
            string userId,
            IReadOnlyList<int> questionIds,
            IReadOnlyDictionary<int, List<int>> selectedAnswerIdsByQuestion);
        TestHistory? GetTestHistoryById(int id, string userId);
        List<TestHistory> GetTestHistoriesByUser(string userId);
        List<TestHistory> GetRecentTestHistories(string userId, int count);
        (int totalQuizzes, double averagePercentage, DateTime? lastQuizDate) GetQuizStatistics(string userId);
        List<TestHistory> GetTestHistoriesByDeck(int deckId);
        List<TestHistory> GetTestHistoriesByContentOwner(string ownerUserId, bool isAdmin);
    }
}
