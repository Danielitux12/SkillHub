namespace SkillHub.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class WorkshopCapacityExceededException : DomainException
{
    public int Capacity { get; }

    public WorkshopCapacityExceededException(string workshopCode, int capacity)
        : base($"Workshop '{workshopCode}' has reached its maximum capacity of {capacity} participants.")
    {
        Capacity = capacity;
    }
}

public class DuplicateEnrollmentException : DomainException
{
    public DuplicateEnrollmentException(string participantEmail, string workshopCode)
        : base($"Participant '{participantEmail}' is already actively enrolled in workshop '{workshopCode}'.")
    {
    }
}

public class ScheduleConflictException : DomainException
{
    public ScheduleConflictException(string participantEmail, string conflictingWorkshopCode, string targetWorkshopCode)
        : base($"Participant '{participantEmail}' has an active schedule conflict between workshop '{conflictingWorkshopCode}' and '{targetWorkshopCode}'.")
    {
    }
}

public class WorkshopAlreadyStartedException : DomainException
{
    public WorkshopAlreadyStartedException(string workshopCode)
        : base($"Workshop '{workshopCode}' has already started or concluded. Action is not permitted.")
    {
    }
}

public class CancellationNotAllowedException : DomainException
{
    public CancellationNotAllowedException(string message) : base(message)
    {
    }
}

public class InvalidWorkshopDatesException : DomainException
{
    public InvalidWorkshopDatesException(string message) : base(message)
    {
    }
}

public class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message)
    {
    }
}
