using SkillHub.Application.DTOs.Reports;

namespace SkillHub.Application.Interfaces.Services;

public interface IReportService
{
    Task<IReadOnlyList<TopWorkshopReportResponse>> GetTopWorkshopsAsync(int count = 10, CancellationToken cancellationToken = default);
}
