using Microsoft.EntityFrameworkCore;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Domain.Entities;
using SkillHub.Infrastructure.Persistence;

namespace SkillHub.Infrastructure.Repositories;

public class WorkshopRepository : IWorkshopRepository
{
    private readonly SkillHubDbContext _context;

    public WorkshopRepository(SkillHubDbContext context)
    {
        _context = context;
    }

    public async Task<Workshop?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Workshops
            .Include(w => w.Instructor)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<Workshop?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return await _context.Workshops
            .Include(w => w.Instructor)
            .FirstOrDefaultAsync(w => w.Code == normalizedCode, cancellationToken);
    }

    public async Task<IReadOnlyList<Workshop>> GetUpcomingWorkshopsAsync(DateTime? referenceTime = null, CancellationToken cancellationToken = default)
    {
        var now = referenceTime ?? DateTime.UtcNow;
        return await _context.Workshops
            .Include(w => w.Instructor)
            .Where(w => w.StartDate > now)
            .OrderBy(w => w.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Workshop>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Workshops
            .Include(w => w.Instructor)
            .OrderBy(w => w.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return await _context.Workshops
            .AnyAsync(w => w.Code == normalizedCode, cancellationToken);
    }

    public async Task AddAsync(Workshop workshop, CancellationToken cancellationToken = default)
    {
        await _context.Workshops.AddAsync(workshop, cancellationToken);
    }
}
