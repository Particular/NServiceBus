namespace NServiceBus.Saga
{
    /// <summary>
    /// Implementors will be invoked when a message arrives that should have been processed
    /// by a saga, but no existing saga was found. This does not include the scenario when
    /// a saga will be created for the given message type.
    /// </summary>
    public interface IHandleSagaNotFound
    {
        /// <summary>
        /// Implementors will implement this method, likely using an injected IBus
        /// to send responses to the client who sent the message.
        /// </summary>
        void Handle(object message);
    }

    /// <summary>
    /// Daniel: Discuss name
    /// </summary>
    public interface IProcessSagaNotFound
    {
        /// <summary>
        /// Implementors will implement this method, likely using an injected IBus
        /// to send responses to the client who sent the message.
        /// </summary>
        void Handle(object message, ISagaNotFoundContext context);
    }

#pragma warning disable 1591
    public interface ISagaNotFoundContext
#pragma warning restore 1591
    {
    }

    class SagaNotFoundContext : ISagaNotFoundContext
    {
    }
}
