namespace QuizManagement.Infrastructure;

public interface ILoginAttemptService
{
    bool IsLockedOut(string email, string ipAddress);
    TimeSpan? GetRemainingLockoutTime(string email, string ipAddress);
    void RecordFailedAttempt(string email, string ipAddress);
    void ClearAttempts(string email, string ipAddress);
}
