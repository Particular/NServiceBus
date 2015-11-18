namespace NServiceBus.Unicast
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Settings;

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
        /// <param name="context">The current context.</param>
        public static Task PublishAsync<T>(BehaviorContext context, Action<T> messageConstructor, NServiceBus.PublishOptions options)
        {
            var mapper = context.Builder.Build<IMessageMapper>();
            return PublishAsync(context, mapper.CreateInstance(messageConstructor), options);
        }

        /// <summary>
        ///  Publish the message to subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="options">The options for the publish.</param>
        /// <param name="context">The current context.</param>
        public static Task PublishAsync(BehaviorContext context, object message, NServiceBus.PublishOptions options)
        {
            var settings = context.Builder.Build<ReadOnlySettings>();
            var pipeline = new PipelineBase<OutgoingPublishContext>(
                context.Builder, 
                settings, 
                settings.Get<PipelineConfiguration>().MainPipeline);

            var publishContext = new OutgoingPublishContext(
                new OutgoingLogicalMessage(message),
                options,
                context);

            return pipeline.Invoke(publishContext);
        }

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="eventType">The type of event to subscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        /// <param name="context">The current context.</param>
        public static Task SubscribeAsync(BehaviorContext context, Type eventType, SubscribeOptions options)
        {
            var settings = context.Builder.Build<ReadOnlySettings>();
            var pipeline = new PipelineBase<SubscribeContext>(context.Builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var subscribeContext = new SubscribeContext(
                context,
                eventType,
                options);

            return pipeline.Invoke(subscribeContext);
        }

        /// <summary>
        /// Unsubscribes to receive published messages of the specified type.
        /// </summary>
        /// <param name="eventType">The type of event to unsubscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        /// <param name="context">The current context.</param>
        public static Task UnsubscribeAsync(BehaviorContext context, Type eventType, UnsubscribeOptions options)
        {
            var settings = context.Builder.Build<ReadOnlySettings>();
            var pipeline = new PipelineBase<UnsubscribeContext>(context.Builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var subscribeContext = new UnsubscribeContext(
                context,
                eventType,
                options);

            return pipeline.Invoke(subscribeContext);
        }

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">The options for the send.</param>
        /// <param name="context">The current context.</param>
        public static Task SendAsync<T>(BehaviorContext context, Action<T> messageConstructor, NServiceBus.SendOptions options)
        {
            var mapper = context.Builder.Build<IMessageMapper>();
            return SendAsync(context, mapper.CreateInstance(messageConstructor), options);
        }

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">The options for the send.</param>
        /// <param name="context">The current context.</param>
        public static Task SendAsync(BehaviorContext context, object message, NServiceBus.SendOptions options)
        {
            var messageType = message.GetType();

            return context.SendMessage(messageType, message, options);
        }

        static Task SendMessage(this BehaviorContext context, Type messageType, object message, NServiceBus.SendOptions options)
        {
            var settings = context.Builder.Build<ReadOnlySettings>();
            var pipeline = new PipelineBase<OutgoingSendContext>(context.Builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var outgoingContext = new OutgoingSendContext(
                new OutgoingLogicalMessage(messageType, message),
                options,
                context);

            return pipeline.Invoke(outgoingContext);
        }

        /// <summary>
        /// Sends the message to the endpoint which sent the message currently being handled on this thread.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">Options for this reply.</param>
        /// <param name="context">The current context.</param>
        public static Task ReplyAsync(BehaviorContext context, object message, NServiceBus.ReplyOptions options)
        {
            var settings = context.Builder.Build<ReadOnlySettings>();
            var pipeline = new PipelineBase<OutgoingReplyContext>(
                context.Builder, 
                settings, 
                settings.Get<PipelineConfiguration>().MainPipeline);

            var outgoingContext = new OutgoingReplyContext(
                new OutgoingLogicalMessage(message),
                options,
                context);

            return pipeline.Invoke(outgoingContext);
        }

        ///  <summary>
        /// Instantiates a message of type T and performs a regular <see cref="ReplyAsync"/>.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">Options for this reply.</param>
        /// <param name="context">The current context.</param>
        public static Task ReplyAsync<T>(BehaviorContext context, Action<T> messageConstructor, NServiceBus.ReplyOptions options)
        {
            var mapper = context.Builder.Build<IMessageMapper>();
            return ReplyAsync(context, mapper.CreateInstance(messageConstructor), options);
        }
    }
}