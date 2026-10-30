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

        services
            .AddOptions<RefreshTokenSettings>()
            .Bind(configuration.GetSection(
                RefreshTokenSettings.SectionName))
            .Validate(
                options =>
                    options.RefreshTokenLifetimeDays > 0,
                "Refresh-token lifetime must be greater than zero.")
            .ValidateOnStart();

        return services;
    }
}
