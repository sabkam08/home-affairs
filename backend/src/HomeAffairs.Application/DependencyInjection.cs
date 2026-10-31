using FluentValidation;
using HomeAffairs.Application.Common.Behaviours;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HomeAffairs.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);

            configuration.AddOpenBehavior(
                typeof(ValidationBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
