using MediatR;

namespace HomeAffairs.Application.Birth.SaveBirthServiceDetails;

public sealed record SaveBirthServiceDetailsCommand(
    Guid ApplicationId,
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
    byte[]? ExpectedRowVersion)
    : IRequest<BirthServiceDetailsResult>;
