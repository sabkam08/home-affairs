using MediatR;

namespace HomeAffairs.Application.Citizens.Dependants;

public sealed record CreateDependantCommand(
    string? SouthAfricanIdNumber,
    string FirstName,
    string? MiddleNames,
    string LastName,
    DateOnly DateOfBirth,
    string Sex,
    string RelationshipType)
    : IRequest<DependantResult>;
