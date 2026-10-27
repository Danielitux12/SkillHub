using Microsoft.EntityFrameworkCore;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Domain.Entities;
using SkillHub.Infrastructure.Persistence;

namespace SkillHub.Infrastructure.Repositories;

public class InstructorRepository : IInstructorRepository
{
    private readonly SkillHubDbContext _context;

    public InstructorRepository(SkillHubDbContext context)
    {
        _context = context;
    }

    public async Task<Instructor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Instructors
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Instructor?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.Instructors
            .FirstOrDefaultAsync(i => i.Email == normalizedEmail, cancellationToken);
    }

    public async Task<IReadOnlyList<Instructor>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Instructors
            .OrderBy(i => i.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Instructor instructor, CancellationToken cancellationToken = default)
    {
        await _context.Instructors.AddAsync(instructor, cancellationToken);
    }
}
