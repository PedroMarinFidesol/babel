using Babel.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Babel.Application;

/// <summary>
/// Extensiones para configurar los servicios de la capa de Application.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra los servicios de Application: MediatR, Validators y Behaviors.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR con behaviors
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline behaviors (el orden importa)
            // 1. Logging: registra todas las requests
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

            // 2. Validation: rechaza requests inválidas antes de procesar
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // 3. Exception: captura excepciones no manejadas (solo para Result)
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>));
        });

        // FluentValidation - auto-registro de validators
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
