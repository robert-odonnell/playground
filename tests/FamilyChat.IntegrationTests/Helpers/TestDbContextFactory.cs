using FamilyChat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyChat.IntegrationTests.Helpers;

public static class TestDbContextFactory
{
    public static FamilyChatDbContext Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<FamilyChatDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new FamilyChatDbContext(options);
    }
}
