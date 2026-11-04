using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using HomeAffairs.Domain.Citizens;
using MediatR;

namespace HomeAffairs.Application.Citizens.ContactDetails;

internal sealed class CreateContactDetailCommandHandler(
    ICitizenRepository citizenRepository,
    IContactDetailRepository contactRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        CreateContactDetailCommand,
        CreateContactDetailResult>
{
    public async Task<CreateContactDetailResult> Handle(
        CreateContactDetailCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var citizen =
            await citizenRepository.GetByUserIdAsync(
                currentUser.UserId.Value,
                cancellationToken);

        if (citizen is null)
        {
            throw new CitizenProfileNotFoundException();
        }

        var contactType =
            request.ContactType
                .Trim()
                .ToUpperInvariant();

        var contactValue =
            NormalizeContactValue(
                contactType,
                request.ContactValue);

        var contactDetail =
            new ContactDetail
            {
                ContactDetailId =
                    Guid.NewGuid(),

                CitizenId =
                    citizen.CitizenId,

                ContactType =
                    contactType,

                ContactValue =
                    contactValue,

                IsPrimary =
                    request.IsPrimary,

                IsVerified =
                    false,

                IsActive =
                    true,

                CreatedAtUtc =
                    clock.UtcNow
            };

        try
        {
            var created =
                await contactRepository.CreateAsync(
                    contactDetail,
                    currentUser.UserId.Value,
                    cancellationToken);

            return Map(created);
        }
        catch (DuplicateContactDetailException)
        {
            throw new ContactDetailAlreadyExistsException();
        }
    }

    private static string NormalizeContactValue(
        string contactType,
        string value)
    {
        var normalized =
            value.Trim();

        return contactType == ContactTypes.Email
            ? normalized.ToLowerInvariant()
            : normalized.Replace(
                " ",
                string.Empty,
                StringComparison.Ordinal);
    }

    private static CreateContactDetailResult Map(
        ContactDetail contact)
    {
        return new CreateContactDetailResult(
            contact.ContactDetailId,
            contact.CitizenId,
            contact.ContactType,
            contact.ContactValue,
            contact.IsPrimary,
            contact.IsVerified,
            contact.IsActive,
            contact.CreatedAtUtc,
            contact.UpdatedAtUtc,
            contact.RowVersion);
    }
}
