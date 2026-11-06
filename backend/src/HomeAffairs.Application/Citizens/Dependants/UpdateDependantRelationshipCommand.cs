using MediatR;

namespace HomeAffairs.Application.Citizens.Dependants;

public sealed record UpdateDependantRelationshipCommand(
    Guid SubjectCitizenId,
    string RelationshipType,
    byte[] ExpectedRowVersion)
    : IRequest<DependantResult>;
