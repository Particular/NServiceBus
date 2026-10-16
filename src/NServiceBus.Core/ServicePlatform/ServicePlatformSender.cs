#nullable enable

namespace NServiceBus.ServicePlatform;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Abstraction for sending messages to the Particular Service Platform.
/// </summary>
/// <typeparam name="TMessage">The type of message to send.</typeparam>
public abstract class ServicePlatformSender<TMessage>
{
    /// <summary>
    /// Sends a message to the Particular Service Platform.
    /// </summary>
    public abstract Task Send(TMessage message, CancellationToken cancellationToken = default);
}
