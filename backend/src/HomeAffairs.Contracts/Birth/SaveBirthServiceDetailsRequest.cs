namespace HomeAffairs.Contracts.Birth;

public sealed record SaveBirthServiceDetailsRequest(
    string? ChildFirstName,
    string? ChildMiddleNames,
    string? ChildLastName,
    DateOnly? ChildDateOfBirth,
    string? ChildSex,
    string? PlaceOfBirth,
    string? ProvinceOfBirth,
    string? CountryOfBirth,
    string? MotherFirstName,
    string? MotherLastName,
    string? MotherIdNumber,
    string? FatherFirstName,
    string? FatherLastName,
    string? FatherIdNumber,
    string? CurrentCertificateNumber,
    string? RequestReason,
    string? RowVersion);
