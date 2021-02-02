namespace NServiceBus.Transport
{
    using System.Threading.Tasks;

    /// <summary>
    /// Allows the transport to push messages to the core.
    /// </summary>
    public interface IMessageReceiver
    {
        /// <summary>
        /// Initializes the receiver.
        /// </summary>
        Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError);

        /// <summary>
        /// Starts receiving messages from the input queue.
        /// </summary>
        Task StartReceive();

        /// <summary>
        /// Stops receiving messages.
        /// </summary>
        Task StopReceive();

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
    public delegate Task<MessageProcessingResult> OnMessage(MessageContext messageContext);


    /// <summary>
    /// Processes a message that has failed processing.
    /// </summary>
    public delegate Task<ErrorHandleResult> OnError(ErrorContext errorContext);
}