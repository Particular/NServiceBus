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

    /// <summary>
    /// Sends a message to the Particular Service Platform as one of the outgoing operations of the message being processed.
    /// The message is dispatched only if processing succeeds, with the same outbox and transaction guarantees as other outgoing messages.
    /// </summary>
    public abstract Task Send(TMessage message, IPipelineContext pipelineContext);
}
