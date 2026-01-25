using MediatR;

namespace Babel.Application.Common;

/// <summary>
/// Interfaz marcadora para commands que modifican estado y no retornan valor.
/// </summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>
/// Interfaz marcadora para commands que modifican estado y retornan un valor.
/// </summary>
/// <typeparam name="TResponse">Tipo del valor retornado.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// Handler base para commands sin valor de retorno.
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

/// <summary>
/// Handler base para commands con valor de retorno.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
