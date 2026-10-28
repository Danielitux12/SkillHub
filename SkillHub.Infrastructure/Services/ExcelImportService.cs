using ClosedXML.Excel;
using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.Interfaces.Services;

namespace SkillHub.Infrastructure.Services;

public class ExcelImportService : IExcelImportService
{
    public Task<IReadOnlyList<ExcelEnrollmentRowDto>> ParseEnrollmentRowsAsync(Stream excelStream, CancellationToken cancellationToken = default)
    {
        var result = new List<ExcelEnrollmentRowDto>();

        using var workbook = new XLWorkbook(excelStream);

        // 1. Target worksheet explicitly named for enrollments/inscripciones
        var worksheet = workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Contains("Enroll", StringComparison.OrdinalIgnoreCase) ||
            w.Name.Contains("Inscrip", StringComparison.OrdinalIgnoreCase));

        // 2. Otherwise search for any worksheet whose header row contains enrollment column names
        if (worksheet == null)
        {
            foreach (var sheet in workbook.Worksheets)
            {
                var row = sheet.FirstRowUsed();
                if (row == null) continue;

                var hasEmail = false;
                var hasCode = false;

                foreach (var cell in row.CellsUsed())
                {
                    var header = cell.GetString().Trim();
                    if (header.Equals("ParticipantEmail", StringComparison.OrdinalIgnoreCase) ||
                        header.Equals("Email", StringComparison.OrdinalIgnoreCase) ||
                        header.Equals("Correo", StringComparison.OrdinalIgnoreCase))
                    {
                        hasEmail = true;
                    }
                    else if (header.Equals("WorkshopCode", StringComparison.OrdinalIgnoreCase) ||
                             header.Equals("Code", StringComparison.OrdinalIgnoreCase) ||
                             header.Equals("Codigo", StringComparison.OrdinalIgnoreCase) ||
                             header.Equals("Taller", StringComparison.OrdinalIgnoreCase))
                    {
                        hasCode = true;
                    }
                }

                if (hasEmail && hasCode)
                {
                    worksheet = sheet;
                    break;
                }
            }
        }

        // 3. Fallback to first worksheet if none matched
        worksheet ??= workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
            return Task.FromResult<IReadOnlyList<ExcelEnrollmentRowDto>>(result);

        var firstRow = worksheet.FirstRowUsed();
        if (firstRow == null)
            return Task.FromResult<IReadOnlyList<ExcelEnrollmentRowDto>>(result);

        var emailColumnIndex = -1;
        var codeColumnIndex = -1;

        foreach (var cell in firstRow.CellsUsed())
        {
            var header = cell.GetString().Trim();
            if (header.Equals("ParticipantEmail", StringComparison.OrdinalIgnoreCase) ||
                header.Equals("Email", StringComparison.OrdinalIgnoreCase) ||
                header.Equals("Correo", StringComparison.OrdinalIgnoreCase))
            {
                emailColumnIndex = cell.Address.ColumnNumber;
            }
            else if (header.Equals("WorkshopCode", StringComparison.OrdinalIgnoreCase) ||
                     header.Equals("Code", StringComparison.OrdinalIgnoreCase) ||
                     header.Equals("Codigo", StringComparison.OrdinalIgnoreCase) ||
                     header.Equals("Taller", StringComparison.OrdinalIgnoreCase))
            {
                codeColumnIndex = cell.Address.ColumnNumber;
            }
        }

        if (emailColumnIndex == -1) emailColumnIndex = 1;
        if (codeColumnIndex == -1) codeColumnIndex = 2;

        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        for (var rowNum = firstRow.RowNumber() + 1; rowNum <= lastRowNumber; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            if (row.IsEmpty())
                continue;

            var email = row.Cell(emailColumnIndex).GetString().Trim();
            var code = row.Cell(codeColumnIndex).GetString().Trim();

            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(code))
                continue;

            result.Add(new ExcelEnrollmentRowDto(rowNum, email, code));
        }

        return Task.FromResult<IReadOnlyList<ExcelEnrollmentRowDto>>(result);
    }
}
