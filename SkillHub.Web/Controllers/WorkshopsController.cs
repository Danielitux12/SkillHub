using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SkillHub.Application.Common.Exceptions;
using SkillHub.Application.DTOs.Workshops;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Application.Interfaces.Services;
using SkillHub.Domain.Exceptions;
using SkillHub.Infrastructure.Identity;
using SkillHub.Web.Common;
using SkillHub.Web.Models.Workshops;

namespace SkillHub.Web.Controllers;

public class WorkshopsController : Controller
{
    private readonly IWorkshopService _workshopService;
    private readonly IInstructorRepository _instructorRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public WorkshopsController(
        IWorkshopService workshopService,
        IInstructorRepository instructorRepository,
        IEnrollmentRepository enrollmentRepository)
    {
        _workshopService = workshopService;
        _instructorRepository = instructorRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? search = null, bool onlyAvailable = false, CancellationToken cancellationToken = default)
    {
        var workshops = await _workshopService.GetAvailableWorkshopsAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            workshops = workshops.Where(w =>
                w.Title.ToLowerInvariant().Contains(term) ||
                w.Code.ToLowerInvariant().Contains(term) ||
                w.InstructorName.ToLowerInvariant().Contains(term) ||
                w.Description.ToLowerInvariant().Contains(term)
            ).ToList();
        }

        if (onlyAvailable)
        {
            workshops = workshops.Where(w => w.AvailableSpots > 0).ToList();
        }

        var viewModel = new WorkshopIndexViewModel
        {
            Workshops = workshops,
            SearchTerm = search,
            OnlyAvailableSpots = onlyAvailable
        };

        return View(viewModel);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var workshop = await _workshopService.GetWorkshopByIdAsync(id, cancellationToken);
        if (workshop == null)
        {
            TempData["Error"] = $"El taller con ID {id} no fue encontrado.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = new WorkshopDetailViewModel
        {
            Workshop = workshop,
            CanEnroll = false,
            IsParticipantEnrolled = false
        };

        var now = DateTime.UtcNow;
        var hasStarted = workshop.StartDate <= now;

        if (User.Identity?.IsAuthenticated == true && User.IsParticipant())
        {
            var participantId = User.GetUserId();
            var existingEnrollment = await _enrollmentRepository.GetActiveEnrollmentAsync(participantId, id, cancellationToken);

            if (existingEnrollment != null)
            {
                viewModel.IsParticipantEnrolled = true;
                viewModel.CanEnroll = false;
                viewModel.CantEnrollReason = "Ya estás inscrito en este taller.";
            }
            else if (hasStarted)
            {
                viewModel.CanEnroll = false;
                viewModel.CantEnrollReason = "Este taller ya ha iniciado o finalizado.";
            }
            else if (workshop.AvailableSpots <= 0)
            {
                viewModel.CanEnroll = false;
                viewModel.CantEnrollReason = "No quedan cupos disponibles para este taller.";
            }
            else
            {
                viewModel.CanEnroll = true;
            }
        }
        else if (User.Identity?.IsAuthenticated == true && User.IsEmployee())
        {
            viewModel.CanEnroll = false;
            viewModel.CantEnrollReason = "Los coordinadores/instructores no se inscriben como participantes.";
        }
        else
        {
            viewModel.CanEnroll = false;
            viewModel.CantEnrollReason = "Inicia sesión con una cuenta de participante para inscribirte.";
        }

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Employee)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var model = new CreateWorkshopViewModel();
        await PopulateInstructorsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Employee)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateWorkshopViewModel model, CancellationToken cancellationToken = default)
    {
        if (model.EndDate <= model.StartDate)
        {
            ModelState.AddModelError(nameof(model.EndDate), "La fecha de fin debe ser posterior a la fecha de inicio.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateInstructorsAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var request = new CreateWorkshopRequest(
                Code: model.Code.Trim().ToUpperInvariant(),
                Title: model.Title.Trim(),
                Description: model.Description.Trim(),
                InstructorEmail: model.InstructorEmail.Trim().ToLowerInvariant(),
                StartDate: DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc),
                EndDate: DateTime.SpecifyKind(model.EndDate, DateTimeKind.Utc),
                Capacity: model.Capacity
            );

            var created = await _workshopService.CreateWorkshopAsync(request, cancellationToken);
            TempData["Success"] = $"¡El taller '{created.Code} - {created.Title}' se creó exitosamente!";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (ConflictException ex)
        {
            ModelState.AddModelError(nameof(model.Code), ex.Message);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error al crear el taller: {ex.Message}");
        }

        await PopulateInstructorsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Employee)]
    public async Task<IActionResult> Enrollments(Guid id, CancellationToken cancellationToken = default)
    {
        var workshop = await _workshopService.GetWorkshopByIdAsync(id, cancellationToken);
        if (workshop == null)
        {
            TempData["Error"] = $"Taller no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        var enrollments = await _workshopService.GetWorkshopEnrollmentsAsync(id, cancellationToken);

        var viewModel = new WorkshopEnrollmentsViewModel
        {
            Workshop = workshop,
            Enrollments = enrollments
        };

        return View(viewModel);
    }

    private async Task PopulateInstructorsAsync(CreateWorkshopViewModel model, CancellationToken cancellationToken)
    {
        var instructors = await _instructorRepository.GetAllAsync(cancellationToken);
        model.AvailableInstructors = instructors.Select(i => new SelectListItem
        {
            Value = i.Email,
            Text = $"{i.FullName} ({i.Specialty}) - {i.Email}"
        }).ToList();
    }
}
