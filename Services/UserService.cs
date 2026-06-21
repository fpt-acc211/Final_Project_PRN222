using BusinessObjects;
using Repositories;

namespace Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public User? GetByEmail(string email) => _repository.GetByEmail(email);

        public User? GetByUsername(string username) => _repository.GetByUsername(username);

        public User? GetById(string id) => _repository.GetById(id);

        public void CreateUser(User user) => _repository.AddUser(user);

        public void UpdateProfile(User user) => _repository.UpdateUser(user);

        public void ChangePassword(User user, string newPasswordHash)
        {
            user.PasswordHash = newPasswordHash;
            user.SecurityStamp = Guid.NewGuid().ToString();
            _repository.UpdateUser(user);
        }
    }
}
