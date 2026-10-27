using SkillHub.Domain.Entities;

namespace SkillHub.Application.Interfaces.Repositories;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Enrollment?> GetActiveEnrollmentAsync(Guid participantId, Guid workshopId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Enrollment>> GetByParticipantIdAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Enrollment>> GetActiveByParticipantIdAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Enrollment>> GetByWorkshopIdAsync(Guid workshopId, CancellationToken cancellationToken = default);
    Task<int> GetActiveCountByWorkshopIdAsync(Guid workshopId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, int>> GetActiveCountsByWorkshopIdsAsync(IEnumerable<Guid> workshopIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Workshop>> GetActiveEnrolledWorkshopsForParticipantAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(Workshop Workshop, int ActiveCount)>> GetTopWorkshopsWithActiveCountsAsync(int limit, CancellationToken cancellationToken = default);
    Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
}
