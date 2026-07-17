namespace Services;

public interface ILoginAttemptLogService
{
    void Log(string email, string ipAddress, bool isSuccess, string? userId = null);
    List<BusinessObjects.LoginAttempt> GetRecent(int count = 200);
    Task<List<BusinessObjects.LoginAttempt>> GetRecentAsync(int count, bool? success);
}
