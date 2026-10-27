using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Infrastructure.Persistence;

namespace SkillHub.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly SkillHubDbContext _context;

    public UnitOfWork(SkillHubDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
