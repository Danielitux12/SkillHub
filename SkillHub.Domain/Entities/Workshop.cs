using SkillHub.Domain.Exceptions;

namespace SkillHub.Domain.Entities;

public class Workshop
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid InstructorId { get; private set; }
    public Instructor? Instructor { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public int Capacity { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public ICollection<Enrollment> Enrollments { get; private set; } = new List<Enrollment>();

    protected Workshop() { }

    public Workshop(
        string code,
        string title,
        string description,
        Guid instructorId,
        DateTime startDate,
        DateTime endDate,
        int capacity,
        Guid? id = null,
        DateTime? referenceTime = null)
    {
        Id = id ?? Guid.NewGuid();
        ValidateDates(startDate, endDate, referenceTime ?? DateTime.UtcNow);
        ValidateBasics(code, title, description, instructorId, capacity);

        Code = code.Trim().ToUpperInvariant();
        Title = title.Trim();
        Description = description.Trim();
        InstructorId = instructorId;
        StartDate = startDate;
        EndDate = endDate;
        Capacity = capacity;
        CreatedAt = referenceTime ?? DateTime.UtcNow;
    }

    public bool HasStarted(DateTime? referenceTime = null)
    {
        var now = referenceTime ?? DateTime.UtcNow;
        return now >= StartDate;
    }

    public int CalculateAvailableSpots(int activeEnrollmentsCount)
    {
        return Math.Max(0, Capacity - activeEnrollmentsCount);
    }

    public bool HasAvailableSpot(int activeEnrollmentsCount)
    {
        return activeEnrollmentsCount < Capacity;
    }

    public bool OverlapsWith(DateTime otherStart, DateTime otherEnd)
    {
        return StartDate < otherEnd && EndDate > otherStart;
    }

    public bool OverlapsWith(Workshop other)
    {
        return OverlapsWith(other.StartDate, other.EndDate);
    }

    private static void ValidateDates(DateTime startDate, DateTime endDate, DateTime referenceTime)
    {
        if (startDate <= referenceTime)
            throw new InvalidWorkshopDatesException($"Workshop start date ({startDate:u}) must be in the future relative to {referenceTime:u}.");

        if (endDate <= startDate)
            throw new InvalidWorkshopDatesException($"Workshop end date ({endDate:u}) must be after start date ({startDate:u}).");
    }

    private static void ValidateBasics(string code, string title, string description, Guid instructorId, int capacity)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainValidationException("Workshop code cannot be empty.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainValidationException("Workshop title cannot be empty.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainValidationException("Workshop description cannot be empty.");

        if (instructorId == Guid.Empty)
            throw new DomainValidationException("A valid instructor must be assigned.");

        if (capacity <= 0)
            throw new DomainValidationException("Workshop capacity must be greater than zero.");
    }
}
