using FluentAssertions;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HomeAffairs.IntegrationTests.Identity;

[Collection(IntegrationTestCollection.Name)]
public sealed class UserPersistenceTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task LockedUntilUtc_should_be_loaded_as_utc()
    {
        var citizen =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await factory.ExecuteSqlAsync(
            $"""
             UPDATE [identity].Users
             SET LockedUntilUtc =
                 DATEADD(
                     MINUTE,
                     15,
                     SYSUTCDATETIME()
                 )
             WHERE UserId = '{citizen.UserId}';
             """);

        using var scope =
            factory.Services.CreateScope();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IUserRepository>();

        var user =
            await repository.GetByIdAsync(
                citizen.UserId,
                CancellationToken.None);

        user.Should().NotBeNull();

        user!.LockedUntilUtc
            .Should()
            .NotBeNull();

        user.LockedUntilUtc!.Value.Offset
            .Should()
            .Be(TimeSpan.Zero);

        user.IsLocked(DateTimeOffset.UtcNow)
            .Should()
            .BeTrue();
    }
}
