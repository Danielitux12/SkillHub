namespace SkillHub.Application.DTOs.Workshops;

public record CreateWorkshopRequest(
    string Code,
    string Title,
    string Description,
    string InstructorEmail,
    DateTime StartDate,
    DateTime EndDate,
    int Capacity
);

public record WorkshopResponse(
    Guid Id,
    string Code,
    string Title,
    string Description,
    Guid InstructorId,
    string InstructorName,
    string InstructorEmail,
    DateTime StartDate,
    DateTime EndDate,
    int Capacity,
    int AvailableSpots
);

public record WorkshopDetailResponse(
    Guid Id,
    string Code,
    string Title,
    string Description,
    Guid InstructorId,
    string InstructorName,
    string InstructorEmail,
    string InstructorSpecialty,
    DateTime StartDate,
    DateTime EndDate,
    int Capacity,
    int AvailableSpots,
    DateTime CreatedAt
);
