namespace SkillHub.Application.DTOs.Reports;

public record TopWorkshopReportResponse(
    Guid WorkshopId,
    string Code,
    string Title,
    string InstructorName,
    int Capacity,
    int ActiveEnrollmentsCount,
    int AvailableSpots,
    DateTime StartDate
);
