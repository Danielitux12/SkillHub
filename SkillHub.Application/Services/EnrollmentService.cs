using SkillHub.Application.Common.Exceptions;
using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Application.Interfaces.Services;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Application.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly IExcelImportService _excelImportService;
    private readonly IUnitOfWork _unitOfWork;

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        IWorkshopRepository workshopRepository,
        IParticipantRepository participantRepository,
        IExcelImportService excelImportService,
        IUnitOfWork unitOfWork)
    {
        _enrollmentRepository = enrollmentRepository;
        _workshopRepository = workshopRepository;
        _participantRepository = participantRepository;
        _excelImportService = excelImportService;
        _unitOfWork = unitOfWork;
    }

    public async Task<EnrollmentResponse> EnrollParticipantAsync(Guid participantId, Guid workshopId, CancellationToken cancellationToken = default)
    {
        var participant = await _participantRepository.GetByIdAsync(participantId, cancellationToken);
        if (participant == null)
            throw new NotFoundException(nameof(Participant), participantId);

        var workshop = await _workshopRepository.GetByIdAsync(workshopId, cancellationToken);
        if (workshop == null)
            throw new NotFoundException(nameof(Workshop), workshopId);

        var now = DateTime.UtcNow;
        if (workshop.HasStarted(now))
            throw new WorkshopAlreadyStartedException(workshop.Code);

        var existingEnrollment = await _enrollmentRepository.GetActiveEnrollmentAsync(participantId, workshopId, cancellationToken);
        if (existingEnrollment != null)
            throw new DuplicateEnrollmentException(participant.Email, workshop.Code);

        var activeCount = await _enrollmentRepository.GetActiveCountByWorkshopIdAsync(workshopId, cancellationToken);
        if (!workshop.HasAvailableSpot(activeCount))
            throw new WorkshopCapacityExceededException(workshop.Code, workshop.Capacity);

        var activeWorkshops = await _enrollmentRepository.GetActiveEnrolledWorkshopsForParticipantAsync(participantId, cancellationToken);
        var conflicting = activeWorkshops.FirstOrDefault(w => w.OverlapsWith(workshop));
        if (conflicting != null)
            throw new ScheduleConflictException(participant.Email, conflicting.Code, workshop.Code);

        var enrollment = new Enrollment(participantId, workshopId, enrollmentDate: now);
        await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new EnrollmentResponse(
            enrollment.Id,
            participant.Id,
            participant.FullName,
            participant.Email,
            workshop.Id,
            workshop.Code,
            workshop.Title,
            workshop.StartDate,
            workshop.EndDate,
            enrollment.EnrollmentDate,
            enrollment.Status.ToString(),
            enrollment.CancelledAt,
            enrollment.CancelledBy
        );
    }

    public async Task<IReadOnlyList<EnrollmentResponse>> GetMyEnrollmentsAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        var enrollments = await _enrollmentRepository.GetByParticipantIdAsync(participantId, cancellationToken);

        return enrollments.Select(e => new EnrollmentResponse(
            e.Id,
            e.ParticipantId,
            e.Participant?.FullName ?? string.Empty,
            e.Participant?.Email ?? string.Empty,
            e.WorkshopId,
            e.Workshop?.Code ?? string.Empty,
            e.Workshop?.Title ?? string.Empty,
            e.Workshop?.StartDate ?? DateTime.MinValue,
            e.Workshop?.EndDate ?? DateTime.MinValue,
            e.EnrollmentDate,
            e.Status.ToString(),
            e.CancelledAt,
            e.CancelledBy
        )).ToList();
    }

    public async Task<EnrollmentResponse> CancelEnrollmentAsync(
        Guid enrollmentId,
        Guid currentUserId,
        string currentUserEmail,
        bool isEmployee,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId, cancellationToken);
        if (enrollment == null)
            throw new NotFoundException(nameof(Enrollment), enrollmentId);

        if (!isEmployee && enrollment.ParticipantId != currentUserId)
            throw new ForbiddenAccessException("You are not authorized to cancel this enrollment.");

        var workshop = enrollment.Workshop ?? await _workshopRepository.GetByIdAsync(enrollment.WorkshopId, cancellationToken);
        if (workshop == null)
            throw new NotFoundException(nameof(Workshop), enrollment.WorkshopId);

        var now = DateTime.UtcNow;

        if (isEmployee)
        {
            enrollment.CancelByEmployee(now, currentUserEmail, workshop);
        }
        else
        {
            enrollment.CancelByParticipant(now, currentUserEmail, workshop);
        }

        await _enrollmentRepository.UpdateAsync(enrollment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new EnrollmentResponse(
            enrollment.Id,
            enrollment.ParticipantId,
            enrollment.Participant?.FullName ?? string.Empty,
            enrollment.Participant?.Email ?? string.Empty,
            workshop.Id,
            workshop.Code,
            workshop.Title,
            workshop.StartDate,
            workshop.EndDate,
            enrollment.EnrollmentDate,
            enrollment.Status.ToString(),
            enrollment.CancelledAt,
            enrollment.CancelledBy
        );
    }

    public async Task<ExcelImportResultDto> ImportEnrollmentsFromExcelAsync(Stream excelStream, CancellationToken cancellationToken = default)
    {
        var rows = await _excelImportService.ParseEnrollmentRowsAsync(excelStream, cancellationToken);
        var errors = new List<ExcelRowErrorDto>();
        var successfulCount = 0;
        var now = DateTime.UtcNow;

        var workshopsCache = new Dictionary<string, Workshop>(StringComparer.OrdinalIgnoreCase);
        var participantsCache = new Dictionary<string, Participant>(StringComparer.OrdinalIgnoreCase);
        var batchActiveCounts = new Dictionary<Guid, int>();
        var batchEnrollmentsByParticipant = new Dictionary<Guid, List<Workshop>>();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.ParticipantEmail))
            {
                errors.Add(new ExcelRowErrorDto(row.RowNumber, row.ParticipantEmail, row.WorkshopCode, "ParticipantEmail is required."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.WorkshopCode))
            {
                errors.Add(new ExcelRowErrorDto(row.RowNumber, row.ParticipantEmail, row.WorkshopCode, "WorkshopCode is required."));
                continue;
            }

            var cleanEmail = row.ParticipantEmail.Trim().ToLowerInvariant();
            var cleanCode = row.WorkshopCode.Trim().ToUpperInvariant();

            if (!participantsCache.TryGetValue(cleanEmail, out var participant))
            {
                participant = await _participantRepository.GetByEmailAsync(cleanEmail, cancellationToken);
                if (participant == null)
                {
                    errors.Add(new ExcelRowErrorDto(row.RowNumber, row.ParticipantEmail, row.WorkshopCode, $"Participant with email '{cleanEmail}' was not found."));
                    continue;
                }
                participantsCache[cleanEmail] = participant;
            }

            if (!workshopsCache.TryGetValue(cleanCode, out var workshop))
            {
                workshop = await _workshopRepository.GetByCodeAsync(cleanCode, cancellationToken);
                if (workshop == null)
                {
                    errors.Add(new ExcelRowErrorDto(row.RowNumber, row.ParticipantEmail, row.WorkshopCode, $"Workshop with code '{cleanCode}' was not found."));
                    continue;
                }
                workshopsCache[cleanCode] = workshop;
            }

            if (workshop.HasStarted(now))
            {
                errors.Add(new ExcelRowErrorDto(row.RowNumber, row.ParticipantEmail, row.WorkshopCode, $"Workshop '{cleanCode}' has already started."));
                continue;
            }

            var existingActive = await _enrollmentRepository.GetActiveEnrollmentAsync(participant.Id, workshop.Id, cancellationToken);
            if (existingActive != null)
            {
                errors.Add(new ExcelRowErrorDto(row.RowNumber, row.ParticipantEmail, row.WorkshopCode, $"Participant is already actively enrolled in workshop '{cleanCode}'."));
                continue;
            }

            if (!batchEnrollmentsByParticipant.TryGetValue(participant.Id, out var participantBatchWorkshops))
            {
                var activeWorkshops = await _enrollmentRepository.GetActiveEnrolledWorkshopsForParticipantAsync(participant.Id, cancellationToken);
                participantBatchWorkshops = activeWorkshops.ToList();
                batchEnrollmentsByParticipant[participant.Id] = participantBatchWorkshops;
            }

            if (participantBatchWorkshops.Any(w => w.Id == workshop.Id))
            {
                errors.Add(new ExcelRowErrorDto(row.RowNumber, row.ParticipantEmail, row.WorkshopCode, $"Duplicate enrollment in batch for workshop '{cleanCode}'."));
                continue;
            }

            var conflicting = participantBatchWorkshops.FirstOrDefault(w => w.OverlapsWith(workshop));
            if (conflicting != null)
            {
                errors.Add(new ExcelRowErrorDto(row.RowNumber, row.ParticipantEmail, row.WorkshopCode, $"Schedule conflict with workshop '{conflicting.Code}'."));
                continue;
            }

            if (!batchActiveCounts.TryGetValue(workshop.Id, out var currentActive))
            {
                currentActive = await _enrollmentRepository.GetActiveCountByWorkshopIdAsync(workshop.Id, cancellationToken);
                batchActiveCounts[workshop.Id] = currentActive;
            }

            if (!workshop.HasAvailableSpot(currentActive))
            {
                errors.Add(new ExcelRowErrorDto(row.RowNumber, row.ParticipantEmail, row.WorkshopCode, $"Workshop '{cleanCode}' capacity of {workshop.Capacity} has been reached."));
                continue;
            }

            var newEnrollment = new Enrollment(participant.Id, workshop.Id, enrollmentDate: now);
            await _enrollmentRepository.AddAsync(newEnrollment, cancellationToken);

            batchActiveCounts[workshop.Id] = currentActive + 1;
            participantBatchWorkshops.Add(workshop);
            successfulCount++;
        }

        if (successfulCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new ExcelImportResultDto(rows.Count, successfulCount, errors.Count, errors);
    }
}
