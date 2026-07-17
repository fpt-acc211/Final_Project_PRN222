using BusinessObjects;

namespace Repositories
{
    public interface IQuestionRepository
    {
        IEnumerable<Question> GetQuestionsByDeckForStudy(int deckId);
        Question? GetQuestionById(int id, string userId, bool allowAll = false);
        void AddQuestion(Question question);
        void AddQuestions(IEnumerable<Question> questions);
        QuestionUpdateResult TryUpdateQuestion(Question question);
        void DeleteQuestion(Question question);
    }
}
