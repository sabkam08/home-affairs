using FluentAssertions;
using NetArchTest.Rules;

namespace HomeAffairs.ArchitectureTests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Domain_Should_Not_Depend_On_Outer_Layers()
    {
        var result = Types
            .InAssembly(
                typeof(HomeAffairs.Domain.Identity.User)
                    .Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "HomeAffairs.Application",
                "HomeAffairs.Infrastructure",
                "HomeAffairs.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types
            .InAssembly(
                typeof(
                    HomeAffairs.Application.DependencyInjection)
                    .Assembly)
            .ShouldNot()
            .HaveDependencyOn(
                "HomeAffairs.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Api()
    {
        var result = Types
            .InAssembly(
                typeof(
                    HomeAffairs.Application.DependencyInjection)
                    .Assembly)
            .ShouldNot()
            .HaveDependencyOn(
                "HomeAffairs.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Dapper()
    {
        var result = Types
            .InAssembly(
                typeof(HomeAffairs.Domain.Identity.User)
                    .Assembly)
            .ShouldNot()
            .HaveDependencyOn("Dapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_SqlClient()
    {
        var result = Types
            .InAssembly(
                typeof(HomeAffairs.Domain.Identity.User)
                    .Assembly)
            .ShouldNot()
            .HaveDependencyOn(
                "Microsoft.Data.SqlClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
