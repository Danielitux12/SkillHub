using SkillHub.Application.DTOs.Enrollments;

namespace SkillHub.Application.Interfaces.Services;

public interface ICertificateService
{
    Task<CertificateFileDto> GetCertificateAsync(Guid enrollmentId, Guid currentUserId, bool isEmployee, CancellationToken cancellationToken = default);
}
