#nullable enable

namespace NServiceBus;

using System.ComponentModel;
using System.Threading.Tasks;

/// <summary>
/// Tells the infrastructure that the user wants to handle a timeout of <typeparamref name="T" />.
/// </summary>
public interface IHandleTimeouts<T> : IHandleTimeouts
{
    /// <summary>
    /// Called when the timeout has expired.
    /// </summary>
    /// <exception cref="System.Exception">This exception will be thrown if <code>null</code> is returned. Return a Task or mark the method as <code>async</code>.</exception>
    Task Timeout(T state, IMessageHandlerContext context);
}

/// <summary>
/// This interface is meant only to provide a common non-generic base type to identify all NServiceBus message handlers that are timeouts.
/// Timeouts must be implemented in a <see cref="Saga&lt;TSagaData&gt;" /> instead.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHandleTimeouts
{
}