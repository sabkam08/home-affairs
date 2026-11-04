using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Citizens.ContactDetails;

internal sealed class DeleteContactDetailCommandHandler(
    IContactDetailRepository contactRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<DeleteContactDetailCommand>
{
    public async Task Handle(
        DeleteContactDetailCommand request,
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

        try
        {
            await contactRepository.DeleteAsync(
                request.ContactDetailId,
                userId,
                request.ExpectedRowVersion,
                clock.UtcNow,
                cancellationToken);
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
}
