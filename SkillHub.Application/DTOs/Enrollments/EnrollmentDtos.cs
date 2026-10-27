namespace SkillHub.Application.DTOs.Enrollments;

public record CreateEnrollmentRequest(
    Guid WorkshopId
);

public record EnrollmentResponse(
    Guid Id,
    Guid ParticipantId,
    string ParticipantName,
    string ParticipantEmail,
    Guid WorkshopId,
    string WorkshopCode,
    string WorkshopTitle,
    DateTime StartDate,
    DateTime EndDate,
    DateTime EnrollmentDate,
    string Status,
    DateTime? CancelledAt,
    string? CancelledBy
);

public record CertificateFileDto(
    byte[] Content,
    string FileName,
    string ContentType
);

public record CertificateModel(
    string CenterName,
    string Title,
    string ParticipantName,
    string ParticipantEmail,
    string WorkshopCode,
    string WorkshopTitle,
    string InstructorName,
    DateTime StartDate,
    DateTime EndDate,
    DateTime EnrollmentDate,
    DateTime GeneratedAt
);

public record ExcelEnrollmentRowDto(
    int RowNumber,
    string ParticipantEmail,
    string WorkshopCode
);

public record ExcelRowErrorDto(
    int RowNumber,
    string ParticipantEmail,
    string WorkshopCode,
    string Reason
);

public record ExcelImportResultDto(
    int TotalProcessed,
    int SuccessfulCount,
    int RejectedCount,
    IReadOnlyList<ExcelRowErrorDto> Errors
);
