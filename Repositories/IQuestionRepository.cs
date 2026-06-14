using BusinessObjects;

namespace Repositories
{
    public interface IQuestionRepository
    {
        IEnumerable<Question> GetQuestionsByDeck(int deckId, string userId);
        Question? GetQuestionById(int id, string userId);
        void AddQuestion(Question question);
        void UpdateQuestion(Question question);
        void DeleteQuestion(Question question);
    }
}
