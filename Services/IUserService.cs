using BusinessObjects;

namespace Services
{
    public interface IUserService
    {
        User? GetByEmail(string email);
        User? GetByUsername(string username);
        User? GetById(string id);
        void CreateUser(User user);
    }
}
