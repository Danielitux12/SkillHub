using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.Interfaces.Services;
using SkillHub.Infrastructure.Identity;
using SkillHub.Web.Models.Reports;

namespace SkillHub.Web.Controllers;

[Authorize(Roles = Roles.Employee)]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<IActionResult> TopWorkshops(int count = 10, CancellationToken cancellationToken = default)
    {
        if (count < 1) count = 10;
        if (count > 50) count = 50;

        var top = await _reportService.GetTopWorkshopsAsync(count, cancellationToken);

        var viewModel = new TopWorkshopsViewModel
        {
            Count = count,
            Workshops = top
        };

        return View(viewModel);
    }
}
