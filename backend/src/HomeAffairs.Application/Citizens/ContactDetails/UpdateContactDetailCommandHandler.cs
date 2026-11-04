using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using HomeAffairs.Domain.Citizens;
using MediatR;

namespace HomeAffairs.Application.Citizens.ContactDetails;

internal sealed class UpdateContactDetailCommandHandler(
    IContactDetailRepository contactRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        UpdateContactDetailCommand,
        UpdateContactDetailResult>
{
    public async Task<UpdateContactDetailResult> Handle(
        UpdateContactDetailCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var userId =
            currentUser.UserId.Value;

        var existing =
            await contactRepository
                .GetByIdForUserAsync(
                    request.ContactDetailId,
                    userId,
                    cancellationToken);

        if (existing is null)
        {
            throw new ContactDetailNotFoundException();
        }

        var contactType =
            request.ContactType
                .Trim()
                .ToUpperInvariant();

        var contactValue =
            NormalizeContactValue(
                contactType,
                request.ContactValue);

        var updated =
            new ContactDetail
            {
                ContactDetailId =
                    existing.ContactDetailId,

                CitizenId =
                    existing.CitizenId,

                ContactType =
                    contactType,

                ContactValue =
                    contactValue,

                IsPrimary =
                    request.IsPrimary,

                IsVerified =
                    existing.IsVerified,

                IsActive =
                    existing.IsActive,

                CreatedAtUtc =
                    existing.CreatedAtUtc,

                UpdatedAtUtc =
                    clock.UtcNow,

                RowVersion =
                    existing.RowVersion
            };

        try
        {
            var persisted =
                await contactRepository.UpdateAsync(
                    updated,
                    userId,
                    request.ExpectedRowVersion,
                    cancellationToken);

            return Map(persisted);
        }
        catch (DuplicateContactDetailException)
        {
            throw new ContactDetailAlreadyExistsException();
        }
        catch (ContactDetailNotFoundPersistenceException)
        {
            throw new ContactDetailNotFoundException();
        }
        catch (ContactDetailConcurrencyException)
        {
            throw new ContactDetailConflictException();
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

    private static UpdateContactDetailResult Map(
        ContactDetail contact)
    {
        return new UpdateContactDetailResult(
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
