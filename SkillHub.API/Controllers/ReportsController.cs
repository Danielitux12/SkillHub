using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.DTOs.Reports;
using SkillHub.Application.Interfaces.Services;
using SkillHub.Infrastructure.Identity;

namespace SkillHub.API.Controllers;

[ApiController]
[Route("api/v1/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("top-workshops")]
    [Authorize(Roles = Roles.Employee)]
    [ProducesResponseType(typeof(IReadOnlyList<TopWorkshopReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTopWorkshops([FromQuery] int count = 10, CancellationToken cancellationToken = default)
    {
        var top = await _reportService.GetTopWorkshopsAsync(count, cancellationToken);
        return Ok(top);
    }
}
