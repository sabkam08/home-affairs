using FluentValidation;
using HomeAffairs.Domain.Applications;

namespace HomeAffairs.Application.Applications.SearchApplications;

public sealed class SearchApplicationsQueryValidator
    : AbstractValidator<SearchApplicationsQuery>
{
    private static readonly IReadOnlySet<string>
        Statuses =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ApplicationStatuses.Draft,
                ApplicationStatuses.Submitted,
                ApplicationStatuses.InProgress,
                ApplicationStatuses.AwaitingInformation,
                ApplicationStatuses.AwaitingApproval,
                ApplicationStatuses.Approved,
                ApplicationStatuses.Rejected,
                ApplicationStatuses.Completed,
                ApplicationStatuses.Cancelled
            };

    public SearchApplicationsQueryValidator()
    {
        RuleFor(query =>
                query.ApplicationNumber)
            .MaximumLength(30);

        When(
            query =>
                !string.IsNullOrWhiteSpace(
                    query.ApplicationType),
            () =>
            {
                RuleFor(query =>
                        query.ApplicationType!)
                    .Must(value =>
                        ApplicationTypes.All.Contains(
                            value.Trim()))
                    .WithMessage(
                        "The application type is not supported.");
            });

        When(
            query =>
                !string.IsNullOrWhiteSpace(
                    query.Status),
            () =>
            {
                RuleFor(query =>
                        query.Status!)
                    .Must(value =>
                        Statuses.Contains(
                            value.Trim()))
                    .WithMessage(
                        "The application status is invalid.");
            });

        RuleFor(query =>
                query.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(query =>
                query.PageSize)
            .InclusiveBetween(
                1,
                100);

        RuleFor(query => query)
            .Must(query =>
                !query.CreatedFromUtc.HasValue
                ||
                !query.CreatedToUtc.HasValue
                ||
                query.CreatedFromUtc.Value <=
                query.CreatedToUtc.Value)
            .WithMessage(
                "Created-from date may not be later " +
                "than created-to date.");
    }
}
