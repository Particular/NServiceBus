using System;
using System.Collections.Generic;

namespace NServiceBus
{
    /// <summary>
    /// Defines a bus to be used with NServiceBus.
    /// </summary>
    public interface IBus : IMessageCreator
    {
        /// <summary>
        /// Publishes the list of messages to subscribers.
        /// If publishing multiple messages, they should all be of the same type
        /// since subscribers are identified by the first message in the list.
        /// </summary>
        /// <param name="messages">A list of messages. The first message's type
        /// is used for looking up subscribers.</param>
        void Publish<T>(params T[] messages);

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        void Publish<T>(Action<T> messageConstructor);

        /// <summary>
        /// Subcribes to recieve published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="messageType">The type of message to subscribe to.</param>
        void Subscribe(Type messageType);

        /// <summary>
        /// Subscribes to recieve published messages of type T.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <typeparam name="T">The type of message to subscribe to.</typeparam>
        void Subscribe<T>();

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// When messages arrive, the condition is evaluated to see if they
        /// should be handled.
        /// </summary>
        /// <param name="messageType">The type of message to subscribe to.</param>
        /// <param name="condition">The condition with which to evaluate messages.</param>
        void Subscribe(Type messageType, Predicate<object> condition);

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// When messages arrive, the condition is evaluated to see if they
        /// should be handled.
        /// </summary>
        /// <typeparam name="T">The type of message to subscribe to.</typeparam>
        /// <param name="condition">The condition with which to evaluate messages.</param>
        void Subscribe<T>(Predicate<T> condition);

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
        /// Sends the list of messages back to the current bus.
        /// </summary>
        /// <param name="messages">The messages to send.</param>
        ICallback SendLocal(params object[] messages);

        /// <summary>
        /// Instantiates a message of type T and sends it back to the current bus.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        ICallback SendLocal<T>(Action<T> messageConstructor);

        /// <summary>
        /// Sends the list of provided messages.
        /// </summary>
        /// <param name="messages">The list of messages to send.</param>
        /// <remarks>
        /// All the messages will be sent to the destination configured for the
        /// first message in the list.
        /// </remarks>
        ICallback Send(params object[] messages);

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
        /// Sends the list of provided messages.
        /// </summary>
        /// <param name="destination">
        /// The address of the destination to which the messages will be sent.
        /// </param>
        /// <param name="messages">The list of messages to send.</param>
        ICallback Send(string destination, params object[] messages);

        /// <summary>
        /// Sends the list of provided messages.
        /// </summary>
        /// <param name="address">
        /// The address to which the messages will be sent.
        /// </param>
        /// <param name="messages">The list of messages to send.</param>
        ICallback Send(Address address, params object[] messages);

        /// <summary>
        /// Instantiates a message of type T and sends it to the given destination.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="destination">The destination to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        /// <returns></returns>
        ICallback Send<T>(string destination, Action<T> messageConstructor);

        /// <summary>
        /// Instantiates a message of type T and sends it to the given address.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="address">The address to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        /// <returns></returns>
        ICallback Send<T>(Address address, Action<T> messageConstructor);

        /// <summary>
        /// Sends the messages to the destination as well as identifying this
        /// as a response to a message containing the Id found in correlationId.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="correlationId"></param>
        /// <param name="messages"></param>
        ICallback Send(string destination, string correlationId, params object[] messages);

        /// <summary>
        /// Sends the messages to the given address as well as identifying this
        /// as a response to a message containing the Id found in correlationId.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="correlationId"></param>
        /// <param name="messages"></param>
        ICallback Send(Address address, string correlationId, params object[] messages);

        /// <summary>
        /// Instantiates a message of the type T using the given messageConstructor,
        /// and sends it to the destination identifying it as a response to a message
        /// containing the Id found in correlationId.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="destination"></param>
        /// <param name="correlationId"></param>
        /// <param name="messageConstructor"></param>
        ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor);

        /// <summary>
        /// Instantiates a message of the type T using the given messageConstructor,
        /// and sends it to the given address identifying it as a response to a message
        /// containing the Id found in correlationId.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="correlationId"></param>
        /// <param name="messageConstructor"></param>
        ICallback Send<T>(Address address, string correlationId, Action<T> messageConstructor);

        /// <summary>
        /// Sends the messages to all sites with matching site keys registered with the gateway.
        /// The gateway is assumed to be located at the master node. 
        /// </summary>
        /// <param name="siteKeys"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        ICallback SendToSites(IEnumerable<string> siteKeys, params object[] messages);

        /// <summary>
        /// Defers the processing of the messages for the given delay. This feature is using the timeout manager so make sure that you enable timeouts
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        ICallback Defer(TimeSpan delay, params object[] messages);

        /// <summary>
        /// Defers the processing of the messages until the specified time. This feature is using the timeout manager so make sure that you enable timeouts
        /// </summary>
        /// <param name="processAt"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        ICallback Defer(DateTime processAt, params object[] messages);

        /// <summary>
        /// Defers the processing of the messages for the given delay. This feature is using the timeout manager so make sure that you enable timeouts
        /// </summary>
        ICallback Defer(TimeSpan delay,Address address, params object[] messages);

        /// <summary>
        /// Defers the processing of the messages until the specified time. This feature is using the timeout manager so make sure that you enable timeouts
        /// </summary>
        ICallback Defer(DateTime processAt, Address address, params object[] messages);

        /// <summary>
        /// Sends all messages to the endpoint which sent the message currently being handled on this thread.
        /// </summary>
        /// <param name="messages">The messages to send.</param>
        void Reply(params object[] messages);

        /// <summary>
        /// Instantiates a message of type T and performs a regular <see cref="Reply"/>.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        void Reply<T>(Action<T> messageConstructor);

        /// <summary>
        /// Returns a completion message with the specified error code to the sender
        /// of the message being handled. The type T can only be an enum or an integer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="errorEnum"></param>
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
        /// <param name="destination"></param>
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
        IInMemoryOperations InMemory { get; }
    }
}
