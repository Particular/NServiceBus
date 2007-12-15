using System;
using NServiceBus.Async;

namespace NServiceBus
{
	/// <summary>
	/// Defines a bus to be used with NServiceBus.
	/// </summary>
    public interface IBus
    {
		/// <summary>
		/// Starts the bus.
		/// </summary>
        void Start();

		/// <summary>
		/// Publishes a message to all subscribers of the the supplied message type.
		/// </summary>
		/// <param name="message">The message to publish.</param>
        void Publish(IMessage message);

        /// <summary>
        /// Publishes the first message in the list to all subscribers of that message type.
        /// </summary>
        /// <param name="messages">A list of messages.  Only the first will be published.</param>
        void Publish(params IMessage[] messages);

		/// <summary>
		/// Subcribes to recieve published messages of the specified type.
		/// </summary>
		/// <param name="messageType">The type of message to subscribe to.</param>
        void Subscribe(Type messageType);

		/// <summary>
		/// Subscribes to receive published messages of the specified type if
		/// they meet the provided condition.
		/// </summary>
		/// <param name="messageType">The type of message to subscribe to.</param>
		/// <param name="condition">The condition under which to receive the message.</param>
        void Subscribe(Type messageType, Predicate<IMessage> condition);
        
		/// <summary>
		/// Unsubscribes from receiving published messages of the specified type.
		/// </summary>
		/// <param name="messageType"></param>
		void Unsubscribe(Type messageType);

		/// <summary>
		/// Sends a message.
		/// </summary>
		/// <param name="message">The message to send.</param>
        void Send(IMessage message);

		/// <summary>
		/// Sends the list of provided messages.
		/// </summary>
		/// <param name="messages">The list of messages to send.</param>
		/// <remarks>
		/// All the messages will be sent to the destination configured for the
		/// first message in the list.
		/// </remarks>
        void Send(params IMessage[] messages);

		/// <summary>
		/// Sends the list of messages back to the current bus.
		/// </summary>
		/// <param name="messages">The messages to send.</param>
        void SendLocal(params IMessage[] messages);

		/// <summary>
		/// Sends the message to the specified destination.
		/// </summary>
		/// <param name="message">The message to send.</param>
		/// <param name="destination">
		/// The address of the destination to send the message to.
		/// </param>
        void Send(IMessage message, string destination);

		/// <summary>
		/// Sends the list of provided messages.
		/// </summary>
		/// <param name="messages">The list of messages to send.</param>
		/// <param name="destination">
		/// The address of the destination to which the messages will be sent.
		/// </param>
        void Send(IMessage[] messages, string destination);

		/// <summary>
		/// Sends a message and calls the provided <see cref="CompletionCallback"/> delegate
		/// when the message is completed.
		/// </summary>
		/// <param name="message">The message to send.</param>
		/// <param name="callback">The delegate to call after the message has completed.</param>
		/// <param name="state">An object containing state data to pass to the delegate method.</param>
		/// <remarks>
		/// A message is completed when the recipient calls the <see cref="Return"/> method on the
		/// bus in the message handler.
		/// </remarks>.
        void Send(IMessage message, CompletionCallback callback, object state);

		/// <summary>
		/// Sends the list of provided messages and calls the provided <see cref="CompletionCallback"/> delegate
		/// when the message is completed.
		/// </summary>
		/// <param name="messages">The list of messages to send.</param>
		/// <param name="callback">The delegate to call after the message has completed.</param>
		/// <param name="state">An object containing state data to pass to the delegate method.</param>
		/// <remarks>
		/// All the messages will be sent to the destination configured for the
		/// first message in the list.
		/// </remarks>
        void Send(IMessage[] messages, CompletionCallback callback, object state);

		/// <summary>
		/// Sends a message and calls the provided <see cref="CompletionCallback"/> delegate
		/// when the message is completed.
		/// </summary>
		/// <param name="message">The message to send.</param>
		/// <param name="destination">The address of the destination to send the message to.</param>
		/// <param name="callback">The delegate to call after the message has completed.</param>
		/// <param name="state">An object containing state data to pass to the delegate method.</param>
		/// <remarks>
		/// A message is completed when the recipient calls the <see cref="Return"/> method on the
		/// bus in the message handler.
		/// </remarks>
        void Send(IMessage message, string destination, CompletionCallback callback, object state);

		/// <summary>
		/// Sends the list of provided messages and calls the provided <see cref="CompletionCallback"/> delegate
		/// when the message is completed.
		/// </summary>
		/// <param name="messages">The list of messages to send.</param>
		/// <param name="destination">The address of the destination to send the messages to.</param>
		/// <param name="callback">The delegate to call after the message has completed.</param>
		/// <param name="state">An object containing state data to pass to the delegate method.</param>
		/// <remarks>
		/// All the messages will be sent to the destination configured for the
		/// first message in the list.
		/// </remarks>
        void Send(IMessage[] messages, string destination, CompletionCallback callback, object state);

		/// <summary>
		/// Sends a messages to the destination found in <see cref="SourceOfMessageBeingHandled"/>.
		/// </summary>
		/// <param name="message">The message to send.</param>
        void Reply(IMessage message);

        /// <summary>
		/// Sends all messages to the destination found in <see cref="SourceOfMessageBeingHandled"/>.
        /// </summary>
        /// <param name="messages">The messages to send.</param>
        void Reply(params IMessage[] messages);


		/// <summary>
		/// Returns an completion message with the specified error code to the sender
		/// of the message being handled.
		/// </summary>
		/// <param name="errorCode">An code specifying the result.</param>
        void Return(int errorCode);

		/// <summary>
		/// Moves the message being handled to the back of the list of available 
		/// messages so it can be handled later.
		/// </summary>
        void HandleCurrentMessageLater();

		/// <summary>
		/// Moves the specified messages to the back of the list of available 
		/// messages so they can be handled later.
		/// </summary>
		/// <param name="messages">The messages to handle later.</param>
        void HandleMessagesLater(params IMessage[] messages);

		/// <summary>
		/// Tells the bus to stop dispatching the current message to additional
		/// handlers.
		/// </summary>
        void DoNotContinueDispatchingCurrentMessageToHandlers();

        /// <summary>
        /// Gets the address from which the message being handled was sent.
        /// </summary>
        string SourceOfMessageBeingHandled { get; }
    }
}
