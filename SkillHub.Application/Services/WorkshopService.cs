using SkillHub.Application.Common.Exceptions;
using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.DTOs.Workshops;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Application.Interfaces.Services;
using SkillHub.Domain.Entities;

namespace SkillHub.Application.Services;

public class WorkshopService : IWorkshopService
{
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IInstructorRepository _instructorRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WorkshopService(
        IWorkshopRepository workshopRepository,
        IInstructorRepository instructorRepository,
        IEnrollmentRepository enrollmentRepository,
        IUnitOfWork unitOfWork)
    {
        _workshopRepository = workshopRepository;
        _instructorRepository = instructorRepository;
        _enrollmentRepository = enrollmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<WorkshopResponse>> GetAvailableWorkshopsAsync(CancellationToken cancellationToken = default)
    {
        var workshops = await _workshopRepository.GetUpcomingWorkshopsAsync(DateTime.UtcNow, cancellationToken);
        if (workshops.Count == 0)
            return Array.Empty<WorkshopResponse>();

        var workshopIds = workshops.Select(w => w.Id);
        var activeCounts = await _enrollmentRepository.GetActiveCountsByWorkshopIdsAsync(workshopIds, cancellationToken);

        return workshops.Select(w =>
        {
            activeCounts.TryGetValue(w.Id, out var activeCount);
            return new WorkshopResponse(
                w.Id,
                w.Code,
                w.Title,
                w.Description,
                w.InstructorId,
                w.Instructor?.FullName ?? string.Empty,
                w.Instructor?.Email ?? string.Empty,
                w.StartDate,
                w.EndDate,
                w.Capacity,
                w.CalculateAvailableSpots(activeCount)
            );
        }).ToList();
    }

    public async Task<WorkshopDetailResponse?> GetWorkshopByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workshop = await _workshopRepository.GetByIdAsync(id, cancellationToken);
        if (workshop == null)
            return null;

        var activeCount = await _enrollmentRepository.GetActiveCountByWorkshopIdAsync(id, cancellationToken);

        return new WorkshopDetailResponse(
            workshop.Id,
            workshop.Code,
            workshop.Title,
            workshop.Description,
            workshop.InstructorId,
            workshop.Instructor?.FullName ?? string.Empty,
            workshop.Instructor?.Email ?? string.Empty,
            workshop.Instructor?.Specialty ?? string.Empty,
            workshop.StartDate,
            workshop.EndDate,
            workshop.Capacity,
            workshop.CalculateAvailableSpots(activeCount),
            workshop.CreatedAt
        );
    }

    public async Task<WorkshopDetailResponse> CreateWorkshopAsync(CreateWorkshopRequest request, CancellationToken cancellationToken = default)
    {
        if (await _workshopRepository.ExistsByCodeAsync(request.Code, cancellationToken))
            throw new ConflictException($"A workshop with code '{request.Code}' already exists.");

        var instructor = await _instructorRepository.GetByEmailAsync(request.InstructorEmail, cancellationToken);
        if (instructor == null)
            throw new NotFoundException($"Instructor with email '{request.InstructorEmail}' does not exist.");

        var workshop = new Workshop(
            request.Code,
            request.Title,
            request.Description,
            instructor.Id,
            request.StartDate,
            request.EndDate,
            request.Capacity
        );

        await _workshopRepository.AddAsync(workshop, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new WorkshopDetailResponse(
            workshop.Id,
            workshop.Code,
            workshop.Title,
            workshop.Description,
            instructor.Id,
            instructor.FullName,
            instructor.Email,
            instructor.Specialty,
            workshop.StartDate,
            workshop.EndDate,
            workshop.Capacity,
            workshop.Capacity,
            workshop.CreatedAt
        );
    }

    public async Task<IReadOnlyList<EnrollmentResponse>> GetWorkshopEnrollmentsAsync(Guid workshopId, CancellationToken cancellationToken = default)
    {
        var workshop = await _workshopRepository.GetByIdAsync(workshopId, cancellationToken);
        if (workshop == null)
            throw new NotFoundException(nameof(Workshop), workshopId);

        var enrollments = await _enrollmentRepository.GetByWorkshopIdAsync(workshopId, cancellationToken);

        return enrollments.Select(e => new EnrollmentResponse(
            e.Id,
            e.ParticipantId,
            e.Participant?.FullName ?? string.Empty,
            e.Participant?.Email ?? string.Empty,
            e.WorkshopId,
            e.Workshop?.Code ?? workshop.Code,
            e.Workshop?.Title ?? workshop.Title,
            e.Workshop?.StartDate ?? workshop.StartDate,
            e.Workshop?.EndDate ?? workshop.EndDate,
            e.EnrollmentDate,
            e.Status.ToString(),
            e.CancelledAt,
            e.CancelledBy
        )).ToList();
    }
}
