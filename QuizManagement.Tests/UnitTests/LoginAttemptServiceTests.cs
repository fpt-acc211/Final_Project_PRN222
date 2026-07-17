using Microsoft.Extensions.Caching.Memory;
using QuizManagement.Infrastructure;
using Xunit;

namespace QuizManagement.Tests.UnitTests;

public class LoginAttemptServiceTests
{
    [Fact]
    public void ConcurrentFailures_ReachTheLockoutThresholdWithoutLostUpdates()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new LoginAttemptService(cache);

        for (var round = 0; round < 25; round++)
        {
            var email = $"student-{round}@test.local";

            Parallel.For(0, 5, _ => service.RecordFailedAttempt(email, "192.0.2.10"));

            Assert.True(service.IsLockedOut(email, "192.0.2.10"));
            Assert.InRange(
                service.GetRemainingLockoutTime(email, "192.0.2.10")!.Value,
                TimeSpan.FromMinutes(14),
                TimeSpan.FromMinutes(15));
        }
    }

    [Fact]
    public void Failures_ArePartitionedByNormalizedEmailAndIp_AndCanBeCleared()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new LoginAttemptService(cache);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            service.RecordFailedAttempt("Student@Test.Local", "192.0.2.10");
        }

        service.RecordFailedAttempt("student@test.local", "192.0.2.11");
        Assert.False(service.IsLockedOut("student@test.local", "192.0.2.10"));
        Assert.False(service.IsLockedOut("student@test.local", "192.0.2.11"));

        service.RecordFailedAttempt("student@test.local", "192.0.2.10");
        Assert.True(service.IsLockedOut("STUDENT@TEST.LOCAL", "192.0.2.10"));
        Assert.False(service.IsLockedOut("student@test.local", "192.0.2.11"));

        service.ClearAttempts("student@test.local", "192.0.2.10");
        Assert.False(service.IsLockedOut("student@test.local", "192.0.2.10"));
        Assert.Null(service.GetRemainingLockoutTime("student@test.local", "192.0.2.10"));
    }
}
