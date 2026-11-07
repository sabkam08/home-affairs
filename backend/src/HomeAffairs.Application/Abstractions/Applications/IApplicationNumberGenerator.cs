namespace HomeAffairs.Application.Abstractions.Applications;

public interface IApplicationNumberGenerator
{
    string Generate(
        DateTimeOffset createdAtUtc);
}
