using BusinessObjects;
using DataAccessObjects;
using System.Collections.Generic;

namespace Repositories
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly QuizManagementDbContext _context;

        public QuestionRepository(QuizManagementDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Question> GetQuestionsByDeck(int deckId, string userId)
            => QuestionDAO.Instance.GetQuestionsByDeck(_context, deckId, userId);

        public Question? GetQuestionById(int id, string userId)
            => QuestionDAO.Instance.GetQuestionById(_context, id, userId);

        public void AddQuestion(Question question)
            => QuestionDAO.Instance.AddQuestion(_context, question);

        public void UpdateQuestion(Question question)
            => QuestionDAO.Instance.UpdateQuestion(_context, question);

        public void DeleteQuestion(Question question)
            => QuestionDAO.Instance.DeleteQuestion(_context, question);
    }
}
