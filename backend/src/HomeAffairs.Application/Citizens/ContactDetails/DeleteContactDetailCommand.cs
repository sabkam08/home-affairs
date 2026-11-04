using MediatR;

namespace HomeAffairs.Application.Citizens.ContactDetails;

public sealed record DeleteContactDetailCommand(
    Guid ContactDetailId,
    byte[] ExpectedRowVersion)
    : IRequest;
