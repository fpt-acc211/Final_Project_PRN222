using BusinessObjects;

namespace Services
{
    public interface IQuestionService
    {
        IEnumerable<Question> GetQuestionsByDeck(int deckId, string userId);
        Question? GetQuestionById(int id, string userId);
        void AddQuestion(Question question);
        void UpdateQuestion(Question question);
        void DeleteQuestion(Question question);
    }
}
