using BusinessObjects;

namespace Services
{
    public interface IQuizService
    {
        List<Question> GetQuestionsForQuiz(int deckId, int questionCount, string userId);
        int GetAvailableQuestionCount(int deckId, string userId);
        TestHistory GradeAndSaveQuiz(int deckId, string userId, List<(int QuestionId, int QuestionType, List<int> SelectedAnswerIds)> submittedAnswers);
        TestHistory? GetTestHistoryById(int id, string userId);
        List<TestHistory> GetTestHistoriesByUser(string userId);
        List<TestHistory> GetRecentTestHistories(string userId, int count);
        (int totalQuizzes, double averagePercentage, DateTime? lastQuizDate) GetQuizStatistics(string userId);
    }
}
