using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Citizens.ContactDetails;

internal sealed class GetMyContactDetailsQueryHandler(
    ICitizenRepository citizenRepository,
    IContactDetailRepository contactRepository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetMyContactDetailsQuery,
        IReadOnlyCollection<
            GetMyContactDetailsResult>>
{
    public async Task<
        IReadOnlyCollection<
            GetMyContactDetailsResult>>
        Handle(
            GetMyContactDetailsQuery request,
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

        var contacts =
            await contactRepository
                .GetByCitizenIdAsync(
                    citizen.CitizenId,
                    cancellationToken);

        return contacts
            .Select(contact =>
                new GetMyContactDetailsResult(
                    contact.ContactDetailId,
                    contact.CitizenId,
                    contact.ContactType,
                    contact.ContactValue,
                    contact.IsPrimary,
                    contact.IsVerified,
                    contact.IsActive,
                    contact.CreatedAtUtc,
                    contact.UpdatedAtUtc,
                    contact.RowVersion))
            .ToArray();
    }
}
