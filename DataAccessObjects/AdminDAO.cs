using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects
{
    public class AdminDAO
    {
        private static AdminDAO? _instance;
        private static readonly object _lock = new();

        private AdminDAO() { }

        public static AdminDAO Instance
        {
            get
            {
                lock (_lock)
                {
                    _instance ??= new AdminDAO();
                    return _instance;
                }
            }
        }

        public (int users, int subjects, int decks, int questions, int testHistories) GetSystemStats(
            QuizManagementDbContext context)
        {
            return (
                context.Users.Count(),
                context.Subjects.Count(),
                context.Decks.Count(),
                context.Questions.Count(),
                context.TestHistories.Count()
            );
        }

        public List<User> GetAllUsers(QuizManagementDbContext context, string? search, string? roleFilter)
        {
            var query = context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(s) ||
                    u.Email.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                query = query.Where(u => u.Role == roleFilter);
            }

            return query.OrderBy(u => u.Username).ToList();
        }

        public User? GetUserById(QuizManagementDbContext context, string id)
        {
            return context.Users.FirstOrDefault(u => u.Id == id);
        }

        public (int subjects, int decks, int questions, int testHistories) GetUserStats(
            QuizManagementDbContext context, string userId)
        {
            return (
                context.Subjects.Count(s => s.UserId == userId),
                context.Decks.Count(d => d.Subject.UserId == userId),
                context.Questions.Count(q => q.Deck.Subject.UserId == userId),
                context.TestHistories.Count(h => h.UserId == userId)
            );
        }

        public void UpdateUser(QuizManagementDbContext context, User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            context.Users.Update(user);
            context.SaveChanges();
        }
    }
}
