using System.Reflection;
using FluentValidation;
using Mapster;
using MapsterMapper;
using MarketplaceAdvisory.Core.Application.Common.Abstractions;
using MarketplaceAdvisory.Core.Application.Common.Behaviors;
using MarketplaceAdvisory.Core.Application.Financial;
using MarketplaceAdvisory.Core.Domain.Financial;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace MarketplaceAdvisory.Core.Application;

/// <summary>
/// Registers the Application layer: MediatR handlers, FluentValidation validators,
/// the validation pipeline behavior and Mapster mapping configuration.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(assembly));

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
        typeAdapterConfig.Scan(assembly);
        services.AddSingleton(typeAdapterConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        // Financial Engine — pure domain services + Principle VIII enforcement point.
        services.AddSingleton<ProfitabilityCalculatorService>();
        services.AddSingleton<IdealPriceSimulator>();
        services.AddSingleton<IProfitabilityFloorGuard, ProfitabilityFloorGuard>();

        return services;
    }
}
