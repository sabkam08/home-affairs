using MediatR;

namespace HomeAffairs.Application.Citizens.Addresses;

public sealed record GetMyAddressesQuery
    : IRequest<
        IReadOnlyCollection<CitizenAddressResult>>;
