using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class LoginAttemptRepository : ILoginAttemptRepository
{
    private readonly QuizManagementDbContext _context;

    public LoginAttemptRepository(QuizManagementDbContext context)
    {
        _context = context;
    }

    public void Log(LoginAttempt attempt) => LoginAttemptDAO.Instance.Log(_context, attempt);
    public List<LoginAttempt> GetRecent(int count = 200) => LoginAttemptDAO.Instance.GetRecent(_context, count);
}
