using Aura.Application.Common.Behaviors;
using Aura.Application.Common.Interfaces;
using Aura.Application.Compatibility;
using Aura.Application.UpgradeRequests.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<IUpgradeFulfillmentService, UpgradeFulfillmentService>();
        services.AddScoped<ICompatibilityScorer, NullCompatibilityScorer>();

        return services;
    }
}
