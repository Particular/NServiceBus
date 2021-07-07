namespace NServiceBus.Pipeline
{
    using Unicast.Messages;

    /// <summary>
    /// A context of handling a logical message by a handler.
    /// </summary>
    public interface IInvokeHandlerContext : IIncomingContext, IMessageHandlerContext
    {
        /// <summary>
        /// The current <see cref="IHandleMessages{T}" /> being executed.
        /// </summary>
        MessageHandler MessageHandler { get; }

        /// <summary>
        /// Message headers.
        /// </summary>
        HeaderDictionary Headers { get; }

        /// <summary>
        /// The message instance being handled.
        /// </summary>
        object MessageBeingHandled { get; }

        /// <summary>
        /// <code>true</code> if <see cref="IMessageHandlerContext.DoNotContinueDispatchingCurrentMessageToHandlers" /> has been called.
        /// </summary>
        bool HandlerInvocationAborted { get; }

        /// <summary>
        /// Metadata for the incoming message.
        /// </summary>
        MessageMetadata MessageMetadata { get; }
    }
}