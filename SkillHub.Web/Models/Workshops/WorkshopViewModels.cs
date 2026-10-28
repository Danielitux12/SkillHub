using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.DTOs.Workshops;

namespace SkillHub.Web.Models.Workshops;

public class WorkshopIndexViewModel
{
    public IReadOnlyList<WorkshopResponse> Workshops { get; set; } = Array.Empty<WorkshopResponse>();
    public string? SearchTerm { get; set; }
    public bool OnlyAvailableSpots { get; set; }
}

public class WorkshopDetailViewModel
{
    public WorkshopDetailResponse Workshop { get; set; } = null!;
    public bool IsParticipantEnrolled { get; set; }
    public bool CanEnroll { get; set; }
    public string? CantEnrollReason { get; set; }
}

public class CreateWorkshopViewModel
{
    [Required(ErrorMessage = "El código del taller es obligatorio.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "El código debe tener entre 3 y 20 caracteres (ej. PHO-101).")]
    [Display(Name = "Código del Taller")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "El título del taller es obligatorio.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "El título debe tener entre 5 y 200 caracteres.")]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción del taller es obligatoria.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "La descripción debe tener entre 10 y 2000 caracteres.")]
    [Display(Name = "Descripción Detallada")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe seleccionar un instructor.")]
    [EmailAddress(ErrorMessage = "El correo del instructor no es válido.")]
    [Display(Name = "Instructor Responsable")]
    public string InstructorEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha y hora de inicio son obligatorias.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Fecha y Hora de Inicio")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(2);

    [Required(ErrorMessage = "La fecha y hora de finalización son obligatorias.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Fecha y Hora de Finalización")]
    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(2).AddHours(3);

    [Required(ErrorMessage = "El cupo máximo es obligatorio.")]
    [Range(1, 1000, ErrorMessage = "El cupo debe ser un valor positivo entre 1 y 1000 participantes.")]
    [Display(Name = "Cupo Máximo")]
    public int Capacity { get; set; } = 20;

    public List<SelectListItem> AvailableInstructors { get; set; } = new();
}

public class WorkshopEnrollmentsViewModel
{
    public WorkshopDetailResponse Workshop { get; set; } = null!;
    public IReadOnlyList<EnrollmentResponse> Enrollments { get; set; } = Array.Empty<EnrollmentResponse>();
}
