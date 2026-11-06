using MediatR;

namespace HomeAffairs.Application.Citizens.Dependants;

public sealed record
    DeactivateDependantRelationshipCommand(
        Guid SubjectCitizenId,
        byte[] ExpectedRowVersion)
    : IRequest;
