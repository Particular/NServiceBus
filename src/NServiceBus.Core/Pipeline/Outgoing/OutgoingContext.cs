namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;

    /// <summary>
    /// The base interface for everything inside the outgoing pipeline.
    /// </summary>
    public abstract class OutgoingContext : BehaviorContext, IOutgoingContext
    {
        /// <summary>
        /// Creates a new instance of an outgoing context.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="parentContext">The parent context.</param>
        protected OutgoingContext(string messageId, Dictionary<string, string> headers, IBehaviorContext parentContext)
            : base(parentContext)
        {
            Headers = headers;
            MessageId = messageId;
        }

        /// <summary>
        /// The id of the outgoing message.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// The headers of the outgoing message.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">The options for the send.</param>
        public Task Send(object message, SendOptions options)
        {
            return BusOperationsBehaviorContext.Send(this, message, options);
        }

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">The options for the send.</param>
        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperationsBehaviorContext.Send(this, messageConstructor, options);
        }

        /// <summary>
        ///  Publish the message to subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="options">The options for the publish.</param>
        public Task Publish(object message, PublishOptions options)
        {
            return BusOperationsBehaviorContext.Publish(this, message, options);
        }

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="publishOptions">Specific options for this event.</param>
        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return BusOperationsBehaviorContext.Publish(this, messageConstructor, publishOptions);
        }

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="eventType">The type of event to subscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return BusOperationsBehaviorContext.Subscribe(this, eventType, options);
        }

        /// <summary>
        /// Unsubscribes to receive published messages of the specified type.
        /// </summary>
        /// <param name="eventType">The type of event to unsubscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return BusOperationsBehaviorContext.Unsubscribe(this, eventType, options);
        }
    }
}