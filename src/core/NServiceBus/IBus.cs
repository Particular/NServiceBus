using System;
using System.Collections.Generic;

namespace NServiceBus
{
	/// <summary>
	/// Defines a bus to be used with NServiceBus.
	/// </summary>
    public interface IBus : IDisposable
    {
		/// <summary>
		/// Starts the bus.
		/// </summary>
        void Start();

	    /// <summary>
	    /// Publishes the list of messages to subscribers.
	    /// If publishing multiple messages, they should all be of the same type
	    /// since subscribers are identified by the first message in the list.
	    /// </summary>
	    /// <param name="messages">A list of messages. The first message's type
	    /// is used for looking up subscribers.</param>
	    void Publish<T>(params T[] messages) where T : IMessage;

		/// <summary>
		/// Subcribes to recieve published messages of the specified type.
		/// </summary>
		/// <param name="messageType">The type of message to subscribe to.</param>
        void Subscribe(Type messageType);

		/// <summary>
		/// Subscribes to receive published messages of the specified type.
		/// When messages arrive, the condition is evaluated to see if they
		/// should be handled.
		/// </summary>
		/// <param name="messageType">The type of message to subscribe to.</param>
		/// <param name="condition">The condition with which to evaluate messages.</param>
        void Subscribe(Type messageType, Predicate<IMessage> condition);
        
		/// <summary>
		/// Unsubscribes from receiving published messages of the specified type.
		/// </summary>
		/// <param name="messageType"></param>
		void Unsubscribe(Type messageType);

        /// <summary>
        /// Sends the list of messages back to the current bus.
        /// </summary>
        /// <param name="messages">The messages to send.</param>
        void SendLocal(params IMessage[] messages);

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
		/// Sends the list of provided messages.
		/// </summary>
        /// <param name="destination">
        /// The address of the destination to which the messages will be sent.
        /// </param>
        /// <param name="messages">The list of messages to send.</param>
        ICallback Send(string destination, params IMessage[] messages);

        /// <summary>
		/// Sends all messages to the destination found in <see cref="SourceOfMessageBeingHandled"/>.
        /// </summary>
        /// <param name="messages">The messages to send.</param>
        void Reply(params IMessage[] messages);


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
        /// Gets the address from which the message being handled was sent.
        /// </summary>
        string SourceOfMessageBeingHandled { get; }

        /// <summary>
        /// Gets the list of key/value pairs that will be in the header of
        /// messages being sent by the same thread.
        /// 
        /// This value will be cleared when a thread receives a message.
        /// </summary>
        IDictionary<string, string> OutgoingHeaders { get; }

        /// <summary>
        /// Gets the list of key/value pairs found in the header of the message
        /// being handled by the current thread.
        /// </summary>
        IDictionary<string, string> IncomingHeaders { get; }
    }
}
