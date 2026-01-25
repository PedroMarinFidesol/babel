using Babel.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Babel.Application.Behaviors;

/// <summary>
/// Behavior que captura excepciones no manejadas y las convierte en Result.Failure.
/// Solo aplica a requests que retornan Result.
/// </summary>
public sealed class ExceptionHandlingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> _logger;

    public ExceptionHandlingBehavior(
        ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (FluentValidation.ValidationException)
        {
            // Re-lanzar excepciones de validación para que sean manejadas por middleware
            throw;
        }
        catch (Exception ex)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogError(
                ex,
                "Excepción no manejada en {RequestName}: {Message}",
                requestName,
                ex.Message);

            var error = new Error(
                "UnhandledException",
                $"Error inesperado: {ex.Message}");

            return CreateFailureResult(error);
        }
    }

    private static TResponse CreateFailureResult(Error error)
    {
        var responseType = typeof(TResponse);

        // Si es Result<T>
        if (responseType.IsGenericType &&
            responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
                .MakeGenericMethod(valueType);

            return (TResponse)failureMethod.Invoke(null, [error])!;
        }

        // Si es Result (sin tipo)
        return (TResponse)(object)Result.Failure(error);
    }
}
