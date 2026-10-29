using FluentValidation;
using HomeAffairs.Application.Identity.Login;
using HomeAffairs.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HomeAffairs.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "An exception occurred while processing request {TraceIdentifier}",
            httpContext.TraceIdentifier);

        var problemDetails = exception switch
        {
            ValidationException validationException =>
                CreateValidationProblemDetails(
                    validationException),

            InvalidCredentialsException =>
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status401Unauthorized,
                    Title = "Authentication failed",
                    Detail =
                        "The supplied email address or password is incorrect."
                },

            DomainException domainException =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Business rule violation",
                    Detail = domainException.Message
                },

            _ => new ProblemDetails
            {
                Status =
                        StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred",
                Detail =
                        "The server encountered an unexpected error."
            }
        };

        problemDetails.Extensions["correlationId"] =
            httpContext.TraceIdentifier;

        httpContext.Response.StatusCode =
            problemDetails.Status
            ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }

    private static ValidationProblemDetails
        CreateValidationProblemDetails(
            ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(error => error.ErrorMessage)
                    .Distinct()
                    .ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Request validation failed",
            Detail =
                "One or more validation errors occurred."
        };
    }
}
