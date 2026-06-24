using BusinessObjects;
using DataAccessObjects;

namespace Repositories
{
    public class QuizRepository : IQuizRepository
    {
        private readonly QuizManagementDbContext _context;

        public QuizRepository(QuizManagementDbContext context)
        {
            _context = context;
        }

        public List<Question> GetQuestionsForQuiz(int deckId)
            => QuizDAO.Instance.GetQuestionsForQuiz(_context, deckId);

        public int GetQuestionCount(int deckId)
            => QuizDAO.Instance.GetQuestionCount(_context, deckId);

        public TestHistory SaveTestResult(TestHistory history)
            => QuizDAO.Instance.SaveTestResult(_context, history);

        public TestHistory? GetTestHistoryById(int id, string userId)
            => QuizDAO.Instance.GetTestHistoryById(_context, id, userId);

        public List<TestHistory> GetTestHistoriesByUser(string userId)
            => QuizDAO.Instance.GetTestHistoriesByUser(_context, userId);

        public List<TestHistory> GetRecentTestHistories(string userId, int count)
            => QuizDAO.Instance.GetRecentTestHistories(_context, userId, count);

        public (int totalQuizzes, double averagePercentage, DateTime? lastQuizDate) GetQuizStatistics(string userId)
            => QuizDAO.Instance.GetQuizStatistics(_context, userId);

        public List<TestHistory> GetTestHistoriesByDeck(int deckId)
            => QuizDAO.Instance.GetTestHistoriesByDeck(_context, deckId);

        public List<TestHistory> GetTestHistoriesByContentOwner(string ownerUserId, bool isAdmin)
            => QuizDAO.Instance.GetTestHistoriesByContentOwner(_context, ownerUserId, isAdmin);
    }
}
