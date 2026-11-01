namespace HomeAffairs.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection
    : ICollectionFixture<HomeAffairsApiFactory>
{
    public const string Name =
        "HomeAffairsIntegrationTests";
}
