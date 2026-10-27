using SkillHub.Application.DTOs.Reports;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Application.Interfaces.Services;

namespace SkillHub.Application.Services;

public class ReportService : IReportService
{
    private readonly IEnrollmentRepository _enrollmentRepository;

    public ReportService(IEnrollmentRepository enrollmentRepository)
    {
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task<IReadOnlyList<TopWorkshopReportResponse>> GetTopWorkshopsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var topResults = await _enrollmentRepository.GetTopWorkshopsWithActiveCountsAsync(count, cancellationToken);

        return topResults.Select(r => new TopWorkshopReportResponse(
            r.Workshop.Id,
            r.Workshop.Code,
            r.Workshop.Title,
            r.Workshop.Instructor?.FullName ?? string.Empty,
            r.Workshop.Capacity,
            r.ActiveCount,
            r.Workshop.CalculateAvailableSpots(r.ActiveCount),
            r.Workshop.StartDate
        )).ToList();
    }
}
