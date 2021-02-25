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
        Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError, OnCompleted onCompleted, CancellationToken cancellationToken);

        /// <summary>
        /// Starts receiving messages from the input queue.
        /// </summary>
        Task StartReceive(CancellationToken cancellationToken);

        /// <summary>
        /// Stops receiving messages.
        /// </summary>
        Task StopReceive(CancellationToken cancellationToken);

        /// <summary>
        /// The <see cref="ISubscriptionManager"/> for this receiver. Will be <c>null</c> if publish-subscribe has been disabled on the <see cref="ReceiveSettings"/>.
        /// </summary>
        ISubscriptionManager Subscriptions { get; }

        /// <summary>
        /// The unique identifier of this instance.
        /// </summary>
        string Id { get; }
    }

    /// <summary>
    /// Processes an incoming message.
    /// </summary>
    public delegate Task OnMessage(MessageContext messageContext, CancellationToken cancellationToken);

    /// <summary>
    /// Processes a message that has failed processing.
    /// </summary>
    public delegate Task<ErrorHandleResult> OnError(ErrorContext errorContext, CancellationToken cancellationToken);

    /// <summary>
    /// Processes a message that has been completed.
    /// </summary>
    public delegate Task OnCompleted(CompleteContext completeContext, CancellationToken cancellationToken);
}