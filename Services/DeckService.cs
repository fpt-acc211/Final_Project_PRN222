using BusinessObjects;
using Repositories;

namespace Services
{
    public class DeckService : IDeckService
    {
        private readonly IDeckRepository _repository;

        public DeckService(IDeckRepository repository)
        {
            _repository = repository;
        }

        public IEnumerable<Deck> GetDecksBySubject(int subjectId, string userId) => _repository.GetDecksBySubject(subjectId, userId);

        public Deck? GetDeckById(int id, string userId) => _repository.GetDeckById(id, userId);

        public bool NameExists(int subjectId, string name, int? excludedId = null) => _repository.NameExists(subjectId, name, excludedId);

        public void AddDeck(Deck deck) => _repository.AddDeck(deck);

        public void UpdateDeck(Deck deck) => _repository.UpdateDeck(deck);

        public void DeleteDeck(Deck deck) => _repository.DeleteDeck(deck);
    }
}
