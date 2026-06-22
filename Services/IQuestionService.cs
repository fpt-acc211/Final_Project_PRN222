using BusinessObjects;

namespace Services
{
    public interface IQuestionService
    {
        IEnumerable<Question> GetQuestionsByDeckForStudy(int deckId);
        IEnumerable<Question> GetQuestionsByDeck(int deckId, string userId);
        Question? GetQuestionById(int id, string userId, bool allowAll = false);
        void AddQuestion(Question question);
        void UpdateQuestion(Question question);
        void DeleteQuestion(Question question);
    }
}
