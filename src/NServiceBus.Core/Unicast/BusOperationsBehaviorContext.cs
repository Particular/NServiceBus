namespace NServiceBus.Unicast
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;

    /// <summary>
    /// Provides access to the bus.
    /// </summary>
    static class BusOperationsBehaviorContext
    {
        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">Specific options for this event.</param>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="context">The current context.</param>
        public static Task Publish<T>(IPipeInlet<OutgoingPublishContext> pipeline, BehaviorContext context, Action<T> messageConstructor, NServiceBus.PublishOptions options)
        {
            var mapper = context.Builder.Build<IMessageMapper>();
            return Publish(pipeline, context, mapper.CreateInstance(messageConstructor), options);
        }

        /// <summary>
        ///  Publish the message to subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="options">The options for the publish.</param>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="context">The current context.</param>
        public static Task Publish(IPipeInlet<OutgoingPublishContext> pipeline, BehaviorContext context, object message, NServiceBus.PublishOptions options)
        {
            var publishContext = new OutgoingPublishContext(
                new OutgoingLogicalMessage(message),
                options,
                context);

            return pipeline.Put(publishContext);
        }

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="eventType">The type of event to subscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="context">The current context.</param>
        public static Task Subscribe(IPipeInlet<SubscribeContext> pipeline, BehaviorContext context, Type eventType, SubscribeOptions options)
        {
            var subscribeContext = new SubscribeContext(
                context,
                eventType,
                options);

            return pipeline.Put(subscribeContext);
        }

        /// <summary>
        /// Unsubscribes to receive published messages of the specified type.
        /// </summary>
        /// <param name="eventType">The type of event to unsubscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="context">The current context.</param>
        public static Task Unsubscribe(IPipeInlet<UnsubscribeContext> pipeline, BehaviorContext context, Type eventType, UnsubscribeOptions options)
        {
            var subscribeContext = new UnsubscribeContext(
                context,
                eventType,
                options);

            return pipeline.Put(subscribeContext);
        }

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">The options for the send.</param>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="context">The current context.</param>
        public static Task Send<T>(IPipeInlet<OutgoingSendContext> pipeline, BehaviorContext context, Action<T> messageConstructor, NServiceBus.SendOptions options)
        {
            var mapper = context.Builder.Build<IMessageMapper>();
            return Send(pipeline, context, mapper.CreateInstance(messageConstructor), options);
        }

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">The options for the send.</param>
        /// <param name="pipeline">The pipelien.</param>
        /// <param name="context">The current context.</param>
        public static Task Send(IPipeInlet<OutgoingSendContext> pipeline, BehaviorContext context, object message, NServiceBus.SendOptions options)
        {
            var messageType = message.GetType();

            return context.SendMessage(pipeline, messageType, message, options);
        }

        static Task SendMessage(this BehaviorContext context, IPipeInlet<OutgoingSendContext> pipeline, Type messageType, object message, NServiceBus.SendOptions options)
        {
            var outgoingContext = new OutgoingSendContext(
                new OutgoingLogicalMessage(messageType, message),
                options,
                context);

            return pipeline.Put(outgoingContext);
        }

        /// <summary>
        /// Sends the message to the endpoint which sent the message currently being handled on this thread.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">Options for this reply.</param>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="context">The current context.</param>
        public static Task Reply(IPipeInlet<OutgoingReplyContext> pipeline, BehaviorContext context, object message, NServiceBus.ReplyOptions options)
        {
            var outgoingContext = new OutgoingReplyContext(
                new OutgoingLogicalMessage(message),
                options,
                context);

            return pipeline.Put(outgoingContext);
        }

        ///  <summary>
        /// Instantiates a message of type T and performs a regular <see cref="Reply"/>.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">Options for this reply.</param>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="context">The current context.</param>
        public static Task Reply<T>(IPipeInlet<OutgoingReplyContext> pipeline, BehaviorContext context, Action<T> messageConstructor, NServiceBus.ReplyOptions options)
        {
            var mapper = context.Builder.Build<IMessageMapper>();
            return Reply(pipeline, context, mapper.CreateInstance(messageConstructor), options);
        }
    }
}