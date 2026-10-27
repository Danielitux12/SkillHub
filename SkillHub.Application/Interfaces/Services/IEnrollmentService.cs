using SkillHub.Application.DTOs.Enrollments;

namespace SkillHub.Application.Interfaces.Services;

public interface IEnrollmentService
{
    Task<EnrollmentResponse> EnrollParticipantAsync(Guid participantId, Guid workshopId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnrollmentResponse>> GetMyEnrollmentsAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<EnrollmentResponse> CancelEnrollmentAsync(Guid enrollmentId, Guid currentUserId, string currentUserEmail, bool isEmployee, CancellationToken cancellationToken = default);
    Task<ExcelImportResultDto> ImportEnrollmentsFromExcelAsync(Stream excelStream, CancellationToken cancellationToken = default);
}
