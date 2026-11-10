using FluentValidation;
using HomeAffairs.Application.Workflow;
using HomeAffairs.Application.Citizens;
using HomeAffairs.Application.Citizens.ContactDetails;
using HomeAffairs.Application.Citizens.Addresses;
using HomeAffairs.Application.Citizens.Dependants;
using HomeAffairs.Application.Identity.Login;
using HomeAffairs.Application.Identity.Refresh;
using HomeAffairs.Application.Applications;
using HomeAffairs.Application.Applications.Approval;
using HomeAffairs.Application.Abstractions.Applications;
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
        if (exception is InvalidCredentialsException)
        {
            logger.LogWarning(
                "Authentication failed for request {TraceIdentifier}",
                httpContext.TraceIdentifier);
        }
        else if (exception is ValidationException)
        {
            logger.LogWarning(
                "Request validation failed for request {TraceIdentifier}",
                httpContext.TraceIdentifier);
        }
        else if (exception is DomainException)
        {
            logger.LogWarning(
                exception,
                "A business rule was violated for request {TraceIdentifier}",
                httpContext.TraceIdentifier);
        }
        else if (exception is InvalidCredentialsException or InvalidRefreshTokenException or RefreshTokenReuseException)
        {
            logger.LogWarning(
                "Authentication-related request failed for {TraceIdentifier}",
                httpContext.TraceIdentifier);
        }
        else
        {
            logger.LogError(
                exception,
                "An unexpected exception occurred for request {TraceIdentifier}",
                httpContext.TraceIdentifier);
        }

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

            InvalidRefreshTokenException =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Token refresh failed",
                    Detail =
                        "The refresh token is invalid or has expired."
                },

            RefreshTokenReuseException =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Refresh token reuse detected",
                    Detail =
                        "Authentication is required again."
                },

            CitizenProfileNotFoundException =>
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status404NotFound,
                    Title =
                        "Citizen profile not found",
                    Detail =
                        "The citizen profile could not be found."
                },

            CitizenProfileAlreadyExistsException =>
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status409Conflict,
                    Title =
                        "Citizen profile already exists",
                    Detail =
                        "A citizen profile already exists " +
                        "for the current user."
                },

            CitizenProfileConcurrencyException =>
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status409Conflict,
                    Title =
                        "Citizen profile conflict",
                    Detail =
                        "The citizen profile was modified " +
                        "by another request. Reload the " +
                        "profile and try again."
                },

            ContactDetailAlreadyExistsException =>
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status409Conflict,
                    Title =
                        "Contact detail already exists",
                    Detail =
                        "This contact detail already exists."
                },

            ContactDetailNotFoundException =>
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status404NotFound,
                    Title =
                        "Contact detail not found",
                    Detail =
                        "The contact detail could not be found."
                },

            ContactDetailConflictException =>
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status409Conflict,
                    Title =
                        "Contact detail conflict",
                    Detail =
                        "The contact detail was modified by " +
                        "another request. Reload it and try again."
                },

            AddressNotFoundException =>
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status404NotFound,
                    Title =
                        "Address not found",
                    Detail =
                        "The address could not be found."
                },  

            AddressConflictException =>
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status409Conflict,
                    Title =
                        "Address conflict",
                    Detail =
                        "The address was modified by another " +
                        "request. Reload it and try again."
                },

            DependantNotFoundException =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Dependant not found",
                    Detail =
                        "The represented citizen could not be found."
                },

            RepresentativeMustBeAdultException =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Adult representative required",
                    Detail =
                        "The representative citizen must be " +
                        "at least 18 years old."
                },

            SubjectMustBeMinorException =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Minor subject required",
                    Detail =
                        "The represented citizen must be under 18."
                },

            DependantRelationshipConflictException =>
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status409Conflict,
                    Title =
                        "Dependant relationship conflict",
                    Detail =
                        "The dependant relationship was modified " +
                        "by another request. Reload it and try again."
                },

            ApplicationNotFoundException =>
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status404NotFound,
                    Title =
                        "Application not found",
                    Detail =
                        "The application could not be found."
                },

            ApplicationSubjectNotFoundException =>
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status404NotFound,
                    Title =
                        "Subject citizen not found",
                    Detail =
                        "The subject citizen could not be found."
                },

            ApplicationSubjectAccessDeniedException =>
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status404NotFound,
                    Title =
                        "Subject citizen not found",
                    Detail =
                        "The subject citizen could not be found."
                },

            ApplicationConflictException =>
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status409Conflict,
                    Title =
                        "Application conflict",
                    Detail =
                        "The application was modified by another " +
                        "request. Reload it and try again."
                },

            InvalidApplicationStateException
                invalidStateException =>
                    new ProblemDetails
                    {
                        Status =
                            StatusCodes.Status409Conflict,
                        Title =
                            "Invalid application state",
                        Detail =
                            invalidStateException.Message
                    },

            ApplicationSelfApprovalException
                selfApprovalException =>
                    new ProblemDetails
                    {
                        Status =
                            StatusCodes.Status409Conflict,
                        Title =
                            "Self-approval is not allowed",
                        Detail =
                            selfApprovalException.Message
                    },

            ApplicationDecisionNotAllowedException
                decisionException =>
                    new ProblemDetails
                    {
                        Status =
                            StatusCodes.Status409Conflict,
                        Title =
                            "Application decision not allowed",
                        Detail =
                            decisionException.Message
                    },

            InvalidApplicationSearchException
                searchException =>
                    new ProblemDetails
                    {
                        Status =
                            StatusCodes.Status400BadRequest,
                        Title =
                            "Invalid application search",
                        Detail =
                            searchException.Message
                    },

            WorkflowDefinitionNotFoundException =>
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status404NotFound,
                    Title =
                        "Workflow definition not found",
                    Detail =
                        "The workflow definition could not be found."
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
