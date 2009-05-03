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
	    void Publish<T>(params T[] messages) where T : IMessage;

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        void Publish<T>(Action<T> messageConstructor) where T : IMessage;

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
        void Subscribe<T>() where T : IMessage;

		/// <summary>
		/// Subscribes to receive published messages of the specified type.
		/// When messages arrive, the condition is evaluated to see if they
		/// should be handled.
		/// </summary>
		/// <param name="messageType">The type of message to subscribe to.</param>
		/// <param name="condition">The condition with which to evaluate messages.</param>
        void Subscribe(Type messageType, Predicate<IMessage> condition);

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// When messages arrive, the condition is evaluated to see if they
        /// should be handled.
        /// </summary>
        /// <typeparam name="T">The type of message to subscribe to.</typeparam>
        /// <param name="condition">The condition with which to evaluate messages.</param>
        void Subscribe<T>(Predicate<T> condition) where T : IMessage;
        
		/// <summary>
		/// Unsubscribes from receiving published messages of the specified type.
		/// </summary>
		/// <param name="messageType"></param>
		void Unsubscribe(Type messageType);

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of message to unsubscribe from.</typeparam>
        void Unsubscribe<T>() where T : IMessage;

        /// <summary>
        /// Sends the list of messages back to the current bus.
        /// </summary>
        /// <param name="messages">The messages to send.</param>
        void SendLocal(params IMessage[] messages);

        /// <summary>
        /// Instantiates a message of type T and sends it back to the current bus.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        void SendLocal<T>(Action<T> messageConstructor) where T : IMessage;

        /// <summary>
		/// Sends the list of provided messages.
		/// </summary>
		/// <param name="messages">The list of messages to send.</param>
		/// <remarks>
		/// All the messages will be sent to the destination configured for the
		/// first message in the list.
		/// </remarks>
        ICallback Send(params IMessage[] messages);

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        /// <remarks>
        /// The message will be sent to the destination configured for T
        /// </remarks>
        ICallback Send<T>(Action<T> messageConstructor) where T : IMessage;

		/// <summary>
		/// Sends the list of provided messages.
		/// </summary>
        /// <param name="destination">
        /// The address of the destination to which the messages will be sent.
        /// </param>
        /// <param name="messages">The list of messages to send.</param>
        ICallback Send(string destination, params IMessage[] messages);

        /// <summary>
        /// Instantiates a message of type T and sends it to the given destination.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="destination">The destination to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        /// <returns></returns>
        ICallback Send<T>(string destination, Action<T> messageConstructor) where T : IMessage;

        /// <summary>
        /// Sends the messages to the destination as well as identifying this
        /// as a response to a message containing the Id found in correlationId.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="correlationId"></param>
        /// <param name="messages"></param>
        void Send(string destination, string correlationId, params IMessage[] messages);

        /// <summary>
        /// Instantiates a message of the type T using the given messageConstructor,
        /// and sends it to the destination identifying it as a response to a message
        /// containing the Id found in correlationId.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="destination"></param>
        /// <param name="correlationId"></param>
        /// <param name="messageConstructor"></param>
        void Send<T>(string destination, string correlationId, Action<T> messageConstructor) where T : IMessage;

        /// <summary>
		/// Sends all messages to the endpoint which sent the message currently being handled on this thread.
        /// </summary>
        /// <param name="messages">The messages to send.</param>
        void Reply(params IMessage[] messages);

        /// <summary>
        /// Instantiates a message of type T and performs a regular <see cref="Reply"/>.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        void Reply<T>(Action<T> messageConstructor) where T : IMessage;


		/// <summary>
		/// Returns a completion message with the specified error code to the sender
		/// of the message being handled.
		/// </summary>
		/// <param name="errorCode">A code specifying the result.</param>
        void Return(int errorCode);

		/// <summary>
		/// Moves the message being handled to the back of the list of available 
		/// messages so it can be handled later.
		/// </summary>
        void HandleCurrentMessageLater();

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
    }
}
