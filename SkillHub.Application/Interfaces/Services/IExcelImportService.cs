using SkillHub.Application.DTOs.Enrollments;

namespace SkillHub.Application.Interfaces.Services;

public interface IExcelImportService
{
    Task<IReadOnlyList<ExcelEnrollmentRowDto>> ParseEnrollmentRowsAsync(Stream excelStream, CancellationToken cancellationToken = default);
}
