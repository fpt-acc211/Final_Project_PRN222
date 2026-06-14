using BusinessObjects;
using Repositories;

namespace Services
{
    public class QuestionService : IQuestionService
    {
        private readonly IQuestionRepository _repository;

        public QuestionService(IQuestionRepository repository)
        {
            _repository = repository;
        }

        public IEnumerable<Question> GetQuestionsByDeck(int deckId, string userId) => _repository.GetQuestionsByDeck(deckId, userId);

        public Question? GetQuestionById(int id, string userId) => _repository.GetQuestionById(id, userId);

        public void AddQuestion(Question question) => _repository.AddQuestion(question);

        public void UpdateQuestion(Question question) => _repository.UpdateQuestion(question);

        public void DeleteQuestion(Question question) => _repository.DeleteQuestion(question);
    }
}
