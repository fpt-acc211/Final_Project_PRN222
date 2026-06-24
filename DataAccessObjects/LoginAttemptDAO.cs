using BusinessObjects;

namespace DataAccessObjects;

public class LoginAttemptDAO
{
    private static LoginAttemptDAO? _instance;
    private static readonly object _lock = new();

    private LoginAttemptDAO() { }

    public static LoginAttemptDAO Instance
    {
        get { lock (_lock) { return _instance ??= new LoginAttemptDAO(); } }
    }

    public void Log(QuizManagementDbContext context, LoginAttempt attempt)
    {
        context.LoginAttempts.Add(attempt);
        context.SaveChanges();
    }

    public List<LoginAttempt> GetRecent(QuizManagementDbContext context, int count = 200)
    {
        return context.LoginAttempts
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToList();
    }
}
