namespace HomeAffairs.Contracts.Citizens;

public sealed record UpdateDependantRelationshipRequest(
    string RelationshipType,
    string RowVersion);
