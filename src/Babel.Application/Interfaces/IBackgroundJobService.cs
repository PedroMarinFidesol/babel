using System.Linq.Expressions;

namespace Babel.Application.Interfaces;

/// <summary>
/// Servicio para encolar trabajos en segundo plano.
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Encola un trabajo para ejecutar inmediatamente.
    /// </summary>
    /// <param name="methodCall">Expresión del método a ejecutar</param>
    /// <returns>ID del trabajo encolado</returns>
    string Enqueue(Expression<Action> methodCall);

    /// <summary>
    /// Encola un trabajo para ejecutar inmediatamente (async).
    /// </summary>
    /// <param name="methodCall">Expresión del método a ejecutar</param>
    /// <returns>ID del trabajo encolado</returns>
    string Enqueue(Expression<Func<Task>> methodCall);

    /// <summary>
    /// Encola un trabajo de un servicio para ejecutar inmediatamente.
    /// </summary>
    /// <typeparam name="T">Tipo del servicio</typeparam>
    /// <param name="methodCall">Expresión del método a ejecutar</param>
    /// <returns>ID del trabajo encolado</returns>
    string Enqueue<T>(Expression<Action<T>> methodCall);

    /// <summary>
    /// Encola un trabajo de un servicio para ejecutar inmediatamente (async).
    /// </summary>
    /// <typeparam name="T">Tipo del servicio</typeparam>
    /// <param name="methodCall">Expresión del método a ejecutar</param>
    /// <returns>ID del trabajo encolado</returns>
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);

    /// <summary>
    /// Programa un trabajo para ejecutar después de un retraso.
    /// </summary>
    /// <typeparam name="T">Tipo del servicio</typeparam>
    /// <param name="methodCall">Expresión del método a ejecutar</param>
    /// <param name="delay">Tiempo de espera antes de ejecutar</param>
    /// <returns>ID del trabajo programado</returns>
    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);

    /// <summary>
    /// Continúa con un trabajo después de que otro termine.
    /// </summary>
    /// <typeparam name="T">Tipo del servicio</typeparam>
    /// <param name="parentId">ID del trabajo padre</param>
    /// <param name="methodCall">Expresión del método a ejecutar</param>
    /// <returns>ID del trabajo de continuación</returns>
    string ContinueWith<T>(string parentId, Expression<Func<T, Task>> methodCall);
}
