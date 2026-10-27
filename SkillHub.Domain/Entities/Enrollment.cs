using SkillHub.Domain.Enums;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Domain.Entities;

public class Enrollment
{
    public Guid Id { get; private set; }
    public Guid ParticipantId { get; private set; }
    public Participant? Participant { get; private set; }
    public Guid WorkshopId { get; private set; }
    public Workshop? Workshop { get; private set; }
    public DateTime EnrollmentDate { get; private set; } = DateTime.UtcNow;
    public EnrollmentStatus Status { get; private set; } = EnrollmentStatus.Active;
    public DateTime? CancelledAt { get; private set; }
    public string? CancelledBy { get; private set; }

    protected Enrollment() { }

    public Enrollment(Guid participantId, Guid workshopId, Guid? id = null, DateTime? enrollmentDate = null)
    {
        if (participantId == Guid.Empty)
            throw new DomainValidationException("Participant ID cannot be empty.");

        if (workshopId == Guid.Empty)
            throw new DomainValidationException("Workshop ID cannot be empty.");

        Id = id ?? Guid.NewGuid();
        ParticipantId = participantId;
        WorkshopId = workshopId;
        EnrollmentDate = enrollmentDate ?? DateTime.UtcNow;
        Status = EnrollmentStatus.Active;
    }

    public void CancelByParticipant(DateTime now, string participantEmail, Workshop workshop)
    {
        ArgumentNullException.ThrowIfNull(workshop);

        if (Status == EnrollmentStatus.Cancelled)
            throw new CancellationNotAllowedException("Enrollment is already cancelled.");

        if (workshop.HasStarted(now))
            throw new WorkshopAlreadyStartedException(workshop.Code);

        var timeUntilStart = workshop.StartDate - now;
        if (timeUntilStart < TimeSpan.FromHours(24))
        {
            throw new CancellationNotAllowedException(
                $"Participants can only cancel up to 24 hours prior to workshop start. Time remaining: {timeUntilStart.TotalHours:F1} hours.");
        }

        Status = EnrollmentStatus.Cancelled;
        CancelledAt = now;
        CancelledBy = string.IsNullOrWhiteSpace(participantEmail) ? "Participant" : participantEmail.Trim();
    }

    public void CancelByEmployee(DateTime now, string employeeEmail, Workshop workshop)
    {
        ArgumentNullException.ThrowIfNull(workshop);

        if (Status == EnrollmentStatus.Cancelled)
            throw new CancellationNotAllowedException("Enrollment is already cancelled.");

        if (workshop.HasStarted(now))
            throw new WorkshopAlreadyStartedException(workshop.Code);

        Status = EnrollmentStatus.Cancelled;
        CancelledAt = now;
        CancelledBy = string.IsNullOrWhiteSpace(employeeEmail) ? "Employee" : employeeEmail.Trim();
    }
}
