namespace HomeAffairs.IntegrationTests.Infrastructure;

[CollectionDefinition(
    Name,
    DisableParallelization = true)]
public sealed class IntegrationTestCollection
    : ICollectionFixture<HomeAffairsApiFactory>
{
    public const string Name =
        "HomeAffairsIntegrationTests";
}
