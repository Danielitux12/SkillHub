using Microsoft.EntityFrameworkCore;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Domain.Entities;
using SkillHub.Infrastructure.Persistence;

namespace SkillHub.Infrastructure.Repositories;

public class ParticipantRepository : IParticipantRepository
{
    private readonly SkillHubDbContext _context;

    public ParticipantRepository(SkillHubDbContext context)
    {
        _context = context;
    }

    public async Task<Participant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Participants
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Participant?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.Participants
            .FirstOrDefaultAsync(p => p.Email == normalizedEmail, cancellationToken);
    }

    public async Task AddAsync(Participant participant, CancellationToken cancellationToken = default)
    {
        await _context.Participants.AddAsync(participant, cancellationToken);
    }

    public Task UpdateAsync(Participant participant, CancellationToken cancellationToken = default)
    {
        _context.Participants.Update(participant);
        return Task.CompletedTask;
    }
}
