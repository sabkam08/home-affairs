using MediatR;

namespace HomeAffairs.Application.Citizens.Addresses;

public sealed record DeleteAddressCommand(
    Guid AddressId,
    byte[] ExpectedRowVersion)
    : IRequest;
