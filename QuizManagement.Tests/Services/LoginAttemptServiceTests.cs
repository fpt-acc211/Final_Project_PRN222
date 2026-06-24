using BusinessObjects;
using Microsoft.Extensions.Caching.Memory;
using QuizManagement.Infrastructure;
using Repositories;
using Services;

namespace QuizManagement.Tests.Services;

public class LoginAttemptServiceTests
{
    [Fact]
    public void RecordFailedAttempt_LocksOutAfterFiveFailures()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new LoginAttemptService(cache);

        for (var i = 0; i < 4; i++)
        {
            service.RecordFailedAttempt("user@test.local", "127.0.0.1");
        }

        Assert.False(service.IsLockedOut("user@test.local", "127.0.0.1"));

        service.RecordFailedAttempt("user@test.local", "127.0.0.1");

        Assert.True(service.IsLockedOut("user@test.local", "127.0.0.1"));
        Assert.True(service.GetRemainingLockoutTime("user@test.local", "127.0.0.1") > TimeSpan.Zero);
    }

    [Fact]
    public void ClearAttempts_RemovesLockout()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new LoginAttemptService(cache);

        for (var i = 0; i < 5; i++)
        {
            service.RecordFailedAttempt("user@test.local", "127.0.0.1");
        }

        service.ClearAttempts("user@test.local", "127.0.0.1");

        Assert.False(service.IsLockedOut("user@test.local", "127.0.0.1"));
    }

    [Fact]
    public void FailedAttempts_AreScopedByEmailAndIpAddress()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new LoginAttemptService(cache);

        for (var i = 0; i < 5; i++)
        {
            service.RecordFailedAttempt("user@test.local", "127.0.0.1");
        }

        Assert.True(service.IsLockedOut("user@test.local", "127.0.0.1"));
        Assert.False(service.IsLockedOut("user@test.local", "127.0.0.2"));
        Assert.False(service.IsLockedOut("other@test.local", "127.0.0.1"));
    }

    [Fact]
    public void LoginAttemptLogService_Log_SavesAttemptFields()
    {
        var repository = new FakeLoginAttemptRepository();
        var service = new LoginAttemptLogService(repository);

        service.Log("user@test.local", "127.0.0.1", isSuccess: true, userId: "user-1");

        var attempt = Assert.Single(repository.Attempts);
        Assert.Equal("user@test.local", attempt.Email);
        Assert.Equal("127.0.0.1", attempt.IpAddress);
        Assert.True(attempt.IsSuccess);
        Assert.Equal("user-1", attempt.UserId);
        Assert.True(DateTime.UtcNow.Subtract(attempt.CreatedAt) < TimeSpan.FromSeconds(5));
    }

    private sealed class FakeLoginAttemptRepository : ILoginAttemptRepository
    {
        public List<LoginAttempt> Attempts { get; } = new();

        public void Log(LoginAttempt attempt) => Attempts.Add(attempt);
        public List<LoginAttempt> GetRecent(int count = 200) => Attempts.Take(count).ToList();
    }
}
