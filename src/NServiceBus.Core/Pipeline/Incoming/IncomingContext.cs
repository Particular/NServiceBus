namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// The base interface for everything after the transport receive phase.
    /// </summary>
    public abstract class IncomingContext : BehaviorContext, IIncomingContext
    {
        /// <summary>
        /// Creates a new instance of an incoming context.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <param name="replyToAddress">The reply to address.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="parentContext">The parent context.</param>
        protected IncomingContext(string messageId, string replyToAddress, IReadOnlyDictionary<string, string> headers, IBehaviorContext parentContext)
            : base(parentContext)
        {

            MessageId = messageId;
            ReplyToAddress = replyToAddress;
            MessageHeaders = headers;
        }

        /// <summary>
        /// The Id of the currently processed message.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// The address of the endpoint that sent the current message being handled.
        /// </summary>
        public string ReplyToAddress { get; }

        /// <summary>
        /// Gets the list of key/value pairs found in the header of the message.
        /// </summary>
        public IReadOnlyDictionary<string, string> MessageHeaders { get; }

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">The options for the send.</param>
        public Task Send(object message, SendOptions options)
        {
            return BusOperations.Send(this, message, options);
        }

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">The options for the send.</param>
        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperations.Send(this, messageConstructor, options);
        }

        /// <summary>
        ///  Publish the message to subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="options">The options for the publish.</param>
        public Task Publish(object message, PublishOptions options)
        {
            return BusOperations.Publish(this, message, options);
        }

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="publishOptions">Specific options for this event.</param>
        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return BusOperations.Publish(this, messageConstructor, publishOptions);
        }

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="eventType">The type of event to subscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return BusOperations.Subscribe(this, eventType, options);
        }

        /// <summary>
        /// Unsubscribes to receive published messages of the specified type.
        /// </summary>
        /// <param name="eventType">The type of event to unsubscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return BusOperations.Unsubscribe(this, eventType, options);
        }

        /// <summary>
        /// Sends the message to the endpoint which sent the message currently being handled.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">Options for this reply.</param>
        public Task Reply(object message, ReplyOptions options)
        {
            return BusOperations.Reply(this, message, options);
        }

        ///  <summary>
        /// Instantiates a message of type T and performs a regular <see cref="Reply"/>.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">Options for this reply.</param>
        public Task Reply<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            return BusOperations.Reply(this, messageConstructor, options);
        }

        /// <summary>
        /// Forwards the current message being handled to the destination maintaining
        /// all of its transport-level properties and headers.
        /// </summary>
        public Task ForwardCurrentMessageTo(string destination)
        {
            return IncomingBusOperations.ForwardCurrentMessageTo(this, destination);
        }
    }
}