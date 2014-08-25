namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines a bus to be used with NServiceBus.
    /// </summary>
    public interface IBus
    {
        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        void Publish<T>(T message);

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        void Publish<T>();

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        void Publish<T>(Action<T> messageConstructor);

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="messageType">The type of message to subscribe to.</param>
        void Subscribe(Type messageType);

        /// <summary>
        /// Subscribes to receive published messages of type T.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <typeparam name="T">The type of message to subscribe to.</typeparam>
        void Subscribe<T>();

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <param name="messageType">The type of message to subscribe to.</param>
        void Unsubscribe(Type messageType);

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of message to unsubscribe from.</typeparam>
        void Unsubscribe<T>();

        /// <summary>
        /// Sends the message back to the current bus.
        /// </summary>
        /// <param name="message">The message to send.</param>
        ICallback SendLocal(object message);

        /// <summary>
        /// Instantiates a message of type T and sends it back to the current bus.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        ICallback SendLocal<T>(Action<T> messageConstructor);

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        ICallback Send(object message);

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        /// <remarks>
        /// The message will be sent to the destination configured for T
        /// </remarks>
        ICallback Send<T>(Action<T> messageConstructor);

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="destination">
        /// The address of the destination to which the message will be sent.
        /// </param>
        /// <param name="message">The message to send.</param>
        ICallback Send(string destination, object message);

        /// <summary>
        /// Instantiates a message of type T and sends it to the given destination.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="destination">The destination to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        ICallback Send<T>(string destination, Action<T> messageConstructor);

        /// <summary>
        /// Sends the message to the destination as well as identifying this
        /// as a response to a message containing the Id found in correlationId.
        /// </summary>
        ICallback Send(string destination, string correlationId, object message);

        /// <summary>
        /// Instantiates a message of the type T using the given messageConstructor,
        /// and sends it to the destination identifying it as a response to a message
        /// containing the Id found in correlationId.
        /// </summary>
        ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor);

        /// <summary>
        /// Sends the message to all sites with matching site keys registered with the gateway.
        /// The gateway is assumed to be located at the master node. 
        /// </summary>
        ICallback SendToSites(IEnumerable<string> siteKeys, object message);

        /// <summary>
        /// Defers the processing of the message for the given delay. This feature is using the timeout manager so make sure that you enable timeouts
        /// </summary>
        ICallback Defer(TimeSpan delay, object message);

        /// <summary>
        /// Defers the processing of the message until the specified time. This feature is using the timeout manager so make sure that you enable timeouts
        /// </summary>
        ICallback Defer(DateTime processAt, object message);

        /// <summary>
        /// Sends the message to the endpoint which sent the message currently being handled on this thread.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void Reply(object message);

        /// <summary>
        /// Instantiates a message of type T and performs a regular <see cref="Reply(object)"/>.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        void Reply<T>(Action<T> messageConstructor);

        /// <summary>
        /// Returns a completion message with the specified error code to the sender
        /// of the message being handled. The type T can only be an enum or an integer.
        /// </summary>
        void Return<T>(T errorEnum);

        /// <summary>
        /// Moves the message being handled to the back of the list of available 
        /// messages so it can be handled later.
        /// </summary>
        void HandleCurrentMessageLater();

        /// <summary>
        /// Forwards the current message being handled to the destination maintaining
        /// all of its transport-level properties and headers.
        /// </summary>
        void ForwardCurrentMessageTo(string destination);

        /// <summary>
        /// Tells the bus to stop dispatching the current message to additional
        /// handlers.
        /// </summary>
        void DoNotContinueDispatchingCurrentMessageToHandlers();

        /// <summary>
        /// Gets the list of key/value pairs that will be in the header of
        /// messages being sent by the same thread.
        /// 
        /// This value will be cleared when a thread receives a message.
        /// </summary>
        IDictionary<string, string> OutgoingHeaders { get; }

        /// <summary>
        /// Gets the message context containing the Id, return address, and headers
        /// of the message currently being handled on this thread.
        /// </summary>
        IMessageContext CurrentMessageContext { get; }

        /// <summary>
        /// Support for in-memory operations.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Message = "Removed to reduce complexity and API confusion. See http://docs.particular.net/nservicebus/inmemoryremoval for more information.")]
        IInMemoryOperations InMemory { get; }
    }
}
