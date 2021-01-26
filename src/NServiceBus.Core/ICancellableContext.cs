namespace NServiceBus
{
    using System.Threading;

    /// <summary>
    /// Provides a <cref see="CancellationToken" /> for the message processing pipeline context hierarchy.
    /// </summary>
    public interface ICancellableContext
    {
        /// <summary>
        /// A <see cref="CancellationToken"/> to observe during message processing. Should be forwarded to other methods that support cancellation.
        /// </summary>
        CancellationToken CancellationToken { get; }
    }
}
