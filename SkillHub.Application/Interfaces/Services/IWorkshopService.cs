using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.DTOs.Workshops;

namespace SkillHub.Application.Interfaces.Services;

public interface IWorkshopService
{
    Task<IReadOnlyList<WorkshopResponse>> GetAvailableWorkshopsAsync(CancellationToken cancellationToken = default);
    Task<WorkshopDetailResponse?> GetWorkshopByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkshopDetailResponse> CreateWorkshopAsync(CreateWorkshopRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnrollmentResponse>> GetWorkshopEnrollmentsAsync(Guid workshopId, CancellationToken cancellationToken = default);
}
