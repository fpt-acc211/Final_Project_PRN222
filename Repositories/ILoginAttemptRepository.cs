using BusinessObjects;

namespace Repositories;

public interface ILoginAttemptRepository
{
    void Log(LoginAttempt attempt);
    List<LoginAttempt> GetRecent(int count = 200);
}
