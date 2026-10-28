using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.Common.Exceptions;
using SkillHub.Application.Interfaces.Services;
using SkillHub.Domain.Exceptions;
using SkillHub.Infrastructure.Identity;
using SkillHub.Web.Common;
using SkillHub.Web.Models.Enrollments;

namespace SkillHub.Web.Controllers;

[Authorize]
public class EnrollmentsController : Controller
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

    [HttpGet]
    [Authorize(Roles = Roles.Participant)]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken = default)
    {
        var participantId = User.GetUserId();
        var enrollments = await _enrollmentService.GetMyEnrollmentsAsync(participantId, cancellationToken);

        var viewModel = new MyEnrollmentsViewModel
        {
            Enrollments = enrollments
        };

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Participant)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(Guid workshopId, CancellationToken cancellationToken = default)
    {
        var participantId = User.GetUserId();
        if (participantId == Guid.Empty)
        {
            TempData["Error"] = "No se pudo identificar la cuenta del participante.";
            return RedirectToAction("Index", "Workshops");
        }

        try
        {
            var enrollment = await _enrollmentService.EnrollParticipantAsync(participantId, workshopId, cancellationToken);
            TempData["Success"] = $"¡Te has inscrito exitosamente en el taller '{enrollment.WorkshopTitle}'!";
            return RedirectToAction(nameof(Mine));
        }
        catch (WorkshopAlreadyStartedException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (DuplicateEnrollmentException ex)
        {
            TempData["Warning"] = ex.Message;
        }
        catch (WorkshopCapacityExceededException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (ScheduleConflictException ex)
        {
            TempData["Error"] = $"Conflicto de horario: {ex.Message}";
        }
        catch (NotFoundException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error inesperado al inscribirse: {ex.Message}";
        }

        return RedirectToAction("Details", "Workshops", new { id = workshopId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var userEmail = User.GetUserEmail();
        var isEmployee = User.IsEmployee();

        try
        {
            var updated = await _enrollmentService.CancelEnrollmentAsync(id, userId, userEmail, isEmployee, cancellationToken);
            TempData["Success"] = $"La inscripción al taller '{updated.WorkshopTitle}' ha sido cancelada correctamente.";
        }
        catch (CancellationNotAllowedException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (WorkshopAlreadyStartedException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (ForbiddenAccessException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al cancelar la inscripción: {ex.Message}";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        if (isEmployee)
        {
            return RedirectToAction("Index", "Workshops");
        }

        return RedirectToAction(nameof(Mine));
    }

    [HttpGet]
    public async Task<IActionResult> Certificate(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var isEmployee = User.IsEmployee();

        try
        {
            var file = await _certificateService.GetCertificateAsync(id, userId, isEmployee, cancellationToken);
            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (ForbiddenAccessException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (BadRequestException ex)
        {
            TempData["Warning"] = ex.Message;
        }
        catch (NotFoundException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar la constancia: {ex.Message}";
        }

        return RedirectToAction(nameof(Mine));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Employee)]
    public IActionResult Import()
    {
        return View(new ExcelImportViewModel());
    }

    [HttpPost]
    [Authorize(Roles = Roles.Employee)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(ExcelImportViewModel model, CancellationToken cancellationToken = default)
    {
        if (model.ExcelFile == null || model.ExcelFile.Length == 0)
        {
            ModelState.AddModelError(nameof(model.ExcelFile), "Por favor seleccione un archivo Excel válido.");
            return View(model);
        }

        var extension = Path.GetExtension(model.ExcelFile.FileName).ToLowerInvariant();
        if (extension != ".xlsx")
        {
            ModelState.AddModelError(nameof(model.ExcelFile), "El formato debe ser un archivo de Excel con extensión .xlsx.");
            return View(model);
        }

        try
        {
            using var stream = model.ExcelFile.OpenReadStream();
            var result = await _enrollmentService.ImportEnrollmentsFromExcelAsync(stream, cancellationToken);
            model.Result = result;

            if (result.SuccessfulCount > 0 && result.RejectedCount == 0)
            {
                TempData["Success"] = $"¡Excelente! Se procesaron {result.TotalProcessed} registros exitosamente sin errores.";
            }
            else if (result.SuccessfulCount > 0 && result.RejectedCount > 0)
            {
                TempData["Warning"] = $"Importación parcial: {result.SuccessfulCount} inscripciones exitosas y {result.RejectedCount} filas rechazadas.";
            }
            else
            {
                TempData["Error"] = $"No se pudo realizar ninguna inscripción. Revise el detalle de {result.RejectedCount} errores a continuación.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al procesar el archivo Excel: {ex.Message}";
        }

        return View(model);
    }
}
