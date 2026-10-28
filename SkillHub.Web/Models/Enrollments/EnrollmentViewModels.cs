using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SkillHub.Application.DTOs.Enrollments;

namespace SkillHub.Web.Models.Enrollments;

public class MyEnrollmentsViewModel
{
    public IReadOnlyList<EnrollmentResponse> Enrollments { get; set; } = Array.Empty<EnrollmentResponse>();
}

public class ExcelImportViewModel
{
    [Required(ErrorMessage = "Por favor seleccione un archivo Excel (.xlsx).")]
    [Display(Name = "Archivo Excel")]
    public IFormFile? ExcelFile { get; set; }

    public ExcelImportResultDto? Result { get; set; }
}
