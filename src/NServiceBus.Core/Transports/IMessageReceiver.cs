namespace NServiceBus.Transport
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Allows the transport to push messages to the core.
    /// </summary>
    public interface IMessageReceiver
    {
        /// <summary>
        /// Initializes the receiver.
        /// </summary>
        Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts receiving messages from the input queue.
        /// </summary>
        Task StartReceive(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops receiving messages.
        /// </summary>
        Task StopReceive(CancellationToken cancellationToken = default);

        /// <summary>
        /// The <see cref="ISubscriptionManager"/> for this receiver. Will be <c>null</c> if publish-subscribe has been disabled on the <see cref="ReceiveSettings"/>.
        /// </summary>
        ISubscriptionManager Subscriptions { get; }

        /// <summary>
        /// The unique identifier of this instance.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The transport address this received is receiving messages from.
        /// </summary>
        string ReceiveAddress { get; }
    }

    /// <summary>
    /// Processes an incoming message.
    /// </summary>
    public delegate Task OnMessage(MessageContext messageContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a message that has failed processing.
    /// </summary>
    public delegate Task<ErrorHandleResult> OnError(ErrorContext errorContext, CancellationToken cancellationToken = default);
}