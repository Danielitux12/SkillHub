using Microsoft.EntityFrameworkCore;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;
using SkillHub.Infrastructure.Persistence;

namespace SkillHub.Infrastructure.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly SkillHubDbContext _context;

    public EnrollmentRepository(SkillHubDbContext context)
    {
        _context = context;
    }

    public async Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Include(e => e.Participant)
            .Include(e => e.Workshop)
                .ThenInclude(w => w!.Instructor)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Enrollment?> GetActiveEnrollmentAsync(Guid participantId, Guid workshopId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Include(e => e.Workshop)
            .FirstOrDefaultAsync(e =>
                e.ParticipantId == participantId &&
                e.WorkshopId == workshopId &&
                e.Status == EnrollmentStatus.Active,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Enrollment>> GetByParticipantIdAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Include(e => e.Participant)
            .Include(e => e.Workshop)
                .ThenInclude(w => w!.Instructor)
            .Where(e => e.ParticipantId == participantId)
            .OrderByDescending(e => e.EnrollmentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Enrollment>> GetActiveByParticipantIdAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Include(e => e.Workshop)
            .Where(e => e.ParticipantId == participantId && e.Status == EnrollmentStatus.Active)
            .OrderBy(e => e.Workshop!.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Enrollment>> GetByWorkshopIdAsync(Guid workshopId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Include(e => e.Participant)
            .Include(e => e.Workshop)
            .Where(e => e.WorkshopId == workshopId)
            .OrderByDescending(e => e.EnrollmentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActiveCountByWorkshopIdAsync(Guid workshopId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .CountAsync(e => e.WorkshopId == workshopId && e.Status == EnrollmentStatus.Active, cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetActiveCountsByWorkshopIdsAsync(IEnumerable<Guid> workshopIds, CancellationToken cancellationToken = default)
    {
        var idList = workshopIds.ToList();
        return await _context.Enrollments
            .Where(e => idList.Contains(e.WorkshopId) && e.Status == EnrollmentStatus.Active)
            .GroupBy(e => e.WorkshopId)
            .Select(g => new { WorkshopId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.WorkshopId, x => x.Count, cancellationToken);
    }

    public async Task<IReadOnlyList<Workshop>> GetActiveEnrolledWorkshopsForParticipantAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Where(e => e.ParticipantId == participantId && e.Status == EnrollmentStatus.Active)
            .Select(e => e.Workshop!)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(Workshop Workshop, int ActiveCount)>> GetTopWorkshopsWithActiveCountsAsync(int limit, CancellationToken cancellationToken = default)
    {
        var query = await _context.Workshops
            .Include(w => w.Instructor)
            .Select(w => new
            {
                Workshop = w,
                ActiveCount = w.Enrollments.Count(e => e.Status == EnrollmentStatus.Active)
            })
            .OrderByDescending(x => x.ActiveCount)
            .ThenBy(x => x.Workshop.Title)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return query.Select(x => (x.Workshop, x.ActiveCount)).ToList();
    }

    public async Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        await _context.Enrollments.AddAsync(enrollment, cancellationToken);
    }

    public Task UpdateAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        _context.Enrollments.Update(enrollment);
        return Task.CompletedTask;
    }
}
