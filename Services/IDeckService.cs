using BusinessObjects;

namespace Services
{
    public interface IDeckService
    {
        IEnumerable<Deck> GetDecksBySubjectForStudy(int subjectId);
        IEnumerable<Deck> GetDecksBySubject(int subjectId, string userId);
        Deck? GetDeckForStudy(int id);
        Deck? GetDeckById(int id, string userId, bool allowAll = false);
        bool NameExists(int subjectId, string name, int? excludedId = null);
        void AddDeck(Deck deck);
        void UpdateDeck(Deck deck);
        void DeleteDeck(Deck deck);
    }
}
