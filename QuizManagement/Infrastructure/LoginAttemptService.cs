using Microsoft.Extensions.Caching.Memory;

namespace QuizManagement.Infrastructure;

public class LoginAttemptService : ILoginAttemptService
{
    private readonly IMemoryCache _cache;
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public LoginAttemptService(IMemoryCache cache)
    {
        _cache = cache;
    }

    // Key scoped per IP+email to prevent one attacker from locking any account.
    private static string AttemptKey(string email, string ip)
        => $"login_attempt:{email.ToLowerInvariant()}:{ip}";

    private static string LockKey(string email, string ip)
        => $"login_lock:{email.ToLowerInvariant()}:{ip}";

    public bool IsLockedOut(string email, string ipAddress)
        => _cache.TryGetValue(LockKey(email, ipAddress), out _);

    public TimeSpan? GetRemainingLockoutTime(string email, string ipAddress)
    {
        if (_cache.TryGetValue(LockKey(email, ipAddress), out DateTimeOffset lockedUntil))
        {
            var remaining = lockedUntil - DateTimeOffset.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : null;
        }
        return null;
    }

    public void RecordFailedAttempt(string email, string ipAddress)
    {
        var attemptKey = AttemptKey(email, ipAddress);
        var count = _cache.GetOrCreate(attemptKey, entry =>
        {
            entry.SlidingExpiration = LockoutDuration;
            return 0;
        });

        count++;

        if (count >= MaxFailedAttempts)
        {
            var lockUntil = DateTimeOffset.UtcNow.Add(LockoutDuration);
            _cache.Set(LockKey(email, ipAddress), lockUntil, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = lockUntil
            });
            _cache.Remove(attemptKey);
        }
        else
        {
            _cache.Set(attemptKey, count, new MemoryCacheEntryOptions
            {
                SlidingExpiration = LockoutDuration
            });
        }
    }

    public void ClearAttempts(string email, string ipAddress)
    {
        _cache.Remove(AttemptKey(email, ipAddress));
        _cache.Remove(LockKey(email, ipAddress));
    }
}
