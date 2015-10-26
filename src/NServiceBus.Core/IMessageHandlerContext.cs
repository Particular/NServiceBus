namespace NServiceBus
{
    using System.Threading.Tasks;

    /// <summary>
    /// The context of the currently processed message for a message handler.
    /// </summary>
    public interface IMessageHandlerContext : IMessageProcessingContext
    {
        /// <summary>
        /// Moves the message being handled to the back of the list of available 
        /// messages so it can be handled later.
        /// </summary>
        Task HandleCurrentMessageLaterAsync();

        /// <summary>
        /// Tells the bus to stop dispatching the current message to additional
        /// handlers.
        /// </summary>
        void DoNotContinueDispatchingCurrentMessageToHandlers();
    }
}