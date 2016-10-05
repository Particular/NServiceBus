namespace NServiceBus.Sagas
{
    using System.Threading.Tasks;

    /// <summary>
    /// Implementations will be invoked when a message arrives that should have been processed
    /// by a saga, but no existing saga was found. This does not include the scenario when
    /// a saga will be created for the given message type.
    /// </summary>
    public interface IHandleSagaNotFound
    {
        /// <summary>
        /// Implementations will implement this method, likely using an injected IBus
        /// to send responses to the client who sent the message.
        /// </summary>
        /// <exception cref="System.Exception">This exception will be thrown if <code>null</code> is returned. Return a Task or mark the method as <code>async</code>.</exception>
        Task Handle(object message, IMessageProcessingContext context);
    }
}