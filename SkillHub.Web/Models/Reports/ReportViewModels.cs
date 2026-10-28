using System.ComponentModel.DataAnnotations;
using SkillHub.Application.DTOs.Reports;

namespace SkillHub.Web.Models.Reports;

public class TopWorkshopsViewModel
{
    [Display(Name = "Cantidad a mostrar")]
    [Range(1, 50, ErrorMessage = "La cantidad debe ser entre 1 y 50.")]
    public int Count { get; set; } = 10;

    public IReadOnlyList<TopWorkshopReportResponse> Workshops { get; set; } = Array.Empty<TopWorkshopReportResponse>();
}
