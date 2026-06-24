using DataAccessObjects;
using Microsoft.EntityFrameworkCore;

namespace QuizManagement.Tests.TestHelpers;

internal static class TestDbContextFactory
{
    public static QuizManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<QuizManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .EnableSensitiveDataLogging()
            .Options;

        return new QuizManagementDbContext(options);
    }
}
