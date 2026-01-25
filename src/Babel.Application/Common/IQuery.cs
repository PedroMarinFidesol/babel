using MediatR;

namespace Babel.Application.Common;

/// <summary>
/// Interfaz marcadora para queries que solo leen datos.
/// Todos los queries retornan Result&lt;T&gt;.
/// </summary>
/// <typeparam name="TResponse">Tipo del valor retornado.</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// Handler base para queries.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
