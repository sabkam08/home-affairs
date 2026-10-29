namespace HomeAffairs.Application.Abstractions.Identity;

public interface IRequestContext
{
    string? IpAddress { get; }

    string? UserAgent { get; }

    string? CorrelationId { get; }
}
