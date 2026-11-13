using HomeAffairs.Application.Abstractions.Birth;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Birth.SaveBirthServiceDetails;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Birth.GetBirthServiceDetails;

internal sealed class GetBirthServiceDetailsQueryHandler(
    IBirthServiceRepository repository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetBirthServiceDetailsQuery,
        BirthServiceDetailsResult>
{
    public async Task<BirthServiceDetailsResult> Handle(
        GetBirthServiceDetailsQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var details =
            await repository.GetForApplicantAsync(
                request.ApplicationId,
                currentUser.UserId.Value,
                cancellationToken);

        if (details is null)
        {
            throw new
                BirthServiceApplicationNotFoundException();
        }

        return SaveBirthServiceDetailsCommandHandler.Map(
            details);
    }
}
