using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.API.Common;
using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.Interfaces.Services;
using SkillHub.Infrastructure.Identity;

namespace SkillHub.API.Controllers;

[ApiController]
[Route("api/v1/enrollments")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly ICertificateService _certificateService;

    public EnrollmentsController(
        IEnrollmentService enrollmentService,
        ICertificateService certificateService)
    {
        _enrollmentService = enrollmentService;
        _certificateService = certificateService;
    }

    [HttpPost]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(EnrollmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enroll([FromBody] CreateEnrollmentRequest request, CancellationToken cancellationToken)
    {
        var participantId = User.GetUserId();
        var enrollment = await _enrollmentService.EnrollParticipantAsync(participantId, request.WorkshopId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, enrollment);
    }

    [HttpGet("mine")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(IReadOnlyList<EnrollmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEnrollments(CancellationToken cancellationToken)
    {
        var participantId = User.GetUserId();
        var enrollments = await _enrollmentService.GetMyEnrollmentsAsync(participantId, cancellationToken);
        return Ok(enrollments);
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize]
    [ProducesResponseType(typeof(EnrollmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var userEmail = User.GetUserEmail();
        var isEmployee = User.IsEmployee();

        var updated = await _enrollmentService.CancelEnrollmentAsync(id, userId, userEmail, isEmployee, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("import")]
    [Authorize(Roles = Roles.Employee)]
    [ProducesResponseType(typeof(ExcelImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ImportFromExcel(IFormFile excelFile, CancellationToken cancellationToken)
    {
        if (excelFile == null || excelFile.Length == 0)
            return BadRequest(new { statusCode = 400, message = "Please provide a valid Excel file." });

        using var stream = excelFile.OpenReadStream();
        var result = await _enrollmentService.ImportEnrollmentsFromExcelAsync(stream, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/certificate")]
    [Authorize]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadCertificate(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var isEmployee = User.IsEmployee();

        var file = await _certificateService.GetCertificateAsync(id, userId, isEmployee, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
