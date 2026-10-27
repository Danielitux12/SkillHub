using SkillHub.Application.Common.Exceptions;
using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Application.Interfaces.Services;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;

namespace SkillHub.Application.Services;

public class CertificateService : ICertificateService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly IPdfGenerator _pdfGenerator;

    public CertificateService(
        IEnrollmentRepository enrollmentRepository,
        IWorkshopRepository workshopRepository,
        IParticipantRepository participantRepository,
        IPdfGenerator pdfGenerator)
    {
        _enrollmentRepository = enrollmentRepository;
        _workshopRepository = workshopRepository;
        _participantRepository = participantRepository;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<CertificateFileDto> GetCertificateAsync(
        Guid enrollmentId,
        Guid currentUserId,
        bool isEmployee,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId, cancellationToken);
        if (enrollment == null)
            throw new NotFoundException(nameof(Enrollment), enrollmentId);

        if (!isEmployee && enrollment.ParticipantId != currentUserId)
            throw new ForbiddenAccessException("You are not authorized to download this certificate.");

        if (enrollment.Status == EnrollmentStatus.Cancelled)
            throw new BadRequestException("Certificate is only available for active enrollments. Cancelled enrollments do not have a certificate.");

        var workshop = enrollment.Workshop ?? await _workshopRepository.GetByIdAsync(enrollment.WorkshopId, cancellationToken);
        if (workshop == null)
            throw new NotFoundException(nameof(Workshop), enrollment.WorkshopId);

        var participant = enrollment.Participant ?? await _participantRepository.GetByIdAsync(enrollment.ParticipantId, cancellationToken);
        if (participant == null)
            throw new NotFoundException(nameof(Participant), enrollment.ParticipantId);

        var model = new CertificateModel(
            CenterName: "SkillHub",
            Title: "Constancia de inscripción",
            ParticipantName: participant.FullName,
            ParticipantEmail: participant.Email,
            WorkshopCode: workshop.Code,
            WorkshopTitle: workshop.Title,
            InstructorName: workshop.Instructor?.FullName ?? "Instructor",
            StartDate: workshop.StartDate,
            EndDate: workshop.EndDate,
            EnrollmentDate: enrollment.EnrollmentDate,
            GeneratedAt: DateTime.UtcNow
        );

        var pdfBytes = await _pdfGenerator.GenerateCertificateAsync(model, cancellationToken);
        var safeParticipantName = string.Join("_", participant.FullName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var safeFileName = $"Constancia_{workshop.Code}_{safeParticipantName}.pdf";

        return new CertificateFileDto(pdfBytes, safeFileName, "application/pdf");
    }
}
