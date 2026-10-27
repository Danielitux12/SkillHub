using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SkillHub.Infrastructure.Persistence;

public class SkillHubDbContextFactory : IDesignTimeDbContextFactory<SkillHubDbContext>
{
    public SkillHubDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SkillHubDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=SkillHubDb;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new SkillHubDbContext(optionsBuilder.Options);
    }
}
