using SkillHub.Application.DTOs.Enrollments;

namespace SkillHub.Application.Interfaces.Services;

public interface IPdfGenerator
{
    Task<byte[]> GenerateCertificateAsync(CertificateModel model, CancellationToken cancellationToken = default);
}
