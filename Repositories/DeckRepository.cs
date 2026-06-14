using BusinessObjects;
using DataAccessObjects;
using System.Collections.Generic;

namespace Repositories
{
    public class DeckRepository : IDeckRepository
    {
        private readonly QuizManagementDbContext _context;

        public DeckRepository(QuizManagementDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Deck> GetDecksBySubject(int subjectId, string userId)
            => DeckDAO.Instance.GetDecksBySubject(_context, subjectId, userId);

        public Deck? GetDeckById(int id, string userId)
            => DeckDAO.Instance.GetDeckById(_context, id, userId);

        public bool NameExists(int subjectId, string name, int? excludedId = null)
            => DeckDAO.Instance.NameExists(_context, subjectId, name, excludedId);

        public void AddDeck(Deck deck)
            => DeckDAO.Instance.AddDeck(_context, deck);

        public void UpdateDeck(Deck deck)
            => DeckDAO.Instance.UpdateDeck(_context, deck);

        public void DeleteDeck(Deck deck)
            => DeckDAO.Instance.DeleteDeck(_context, deck);
    }
}
