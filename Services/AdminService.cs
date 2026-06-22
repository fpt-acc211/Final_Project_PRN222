using BusinessObjects;
using Repositories;

namespace Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _repository;

        public AdminService(IAdminRepository repository)
        {
            _repository = repository;
        }

        public (int users, int subjects, int decks, int questions, int testHistories) GetSystemStats()
            => _repository.GetSystemStats();

        public List<User> GetAllUsers(string? search = null, string? roleFilter = null)
            => _repository.GetAllUsers(search, roleFilter);

        public User? GetUserById(string id) => _repository.GetUserById(id);

        public (int subjects, int decks, int questions, int testHistories) GetUserStats(string userId)
            => _repository.GetUserStats(userId);

        public int CountActiveAdmins() => _repository.CountActiveAdmins();

        public void ChangeRole(User user, string newRole)
        {
            user.Role = newRole;
            user.SecurityStamp = Guid.NewGuid().ToString();
            _repository.UpdateUser(user);
        }

        public void SetDisabled(User user, bool disabled)
        {
            user.IsDisabled = disabled;
            user.SecurityStamp = Guid.NewGuid().ToString();
            _repository.UpdateUser(user);
        }
    }
}
