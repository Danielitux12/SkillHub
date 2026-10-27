using System.Net;
using System.Text.Json;
using SkillHub.Application.Common.Exceptions;
using SkillHub.Domain.Exceptions;

namespace SkillHub.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            NotFoundException notFoundEx => ((int)HttpStatusCode.NotFound, notFoundEx.Message),
            ForbiddenAccessException forbiddenEx => ((int)HttpStatusCode.Forbidden, forbiddenEx.Message),
            ConflictException conflictEx => ((int)HttpStatusCode.Conflict, conflictEx.Message),
            DuplicateEnrollmentException dupEx => ((int)HttpStatusCode.Conflict, dupEx.Message),
            BadRequestException badReqEx => ((int)HttpStatusCode.BadRequest, badReqEx.Message),
            DomainValidationException validationEx => ((int)HttpStatusCode.BadRequest, validationEx.Message),
            InvalidWorkshopDatesException datesEx => ((int)HttpStatusCode.BadRequest, datesEx.Message),
            WorkshopCapacityExceededException capacityEx => ((int)HttpStatusCode.BadRequest, capacityEx.Message),
            ScheduleConflictException conflictEx => ((int)HttpStatusCode.BadRequest, conflictEx.Message),
            CancellationNotAllowedException cancelEx => ((int)HttpStatusCode.BadRequest, cancelEx.Message),
            WorkshopAlreadyStartedException startedEx => ((int)HttpStatusCode.BadRequest, startedEx.Message),
            _ => HandleUnhandledException(exception)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new
        {
            statusCode,
            message
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }

    private (int statusCode, string message) HandleUnhandledException(Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred during request execution: {Message}", exception.Message);
        return ((int)HttpStatusCode.InternalServerError, "An unexpected internal server error occurred.");
    }
}
