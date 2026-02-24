using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FamilyChat.Infrastructure.Persistence;

public sealed class FamilyChatDbContextFactory : IDesignTimeDbContextFactory<FamilyChatDbContext>
{
    public FamilyChatDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FamilyChatDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=FamilyChat;User Id=sa;Password=Your_password123;TrustServerCertificate=True;");

        return new FamilyChatDbContext(optionsBuilder.Options);
    }
}
