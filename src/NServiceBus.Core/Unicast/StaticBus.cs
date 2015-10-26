namespace NServiceBus.Unicast
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    internal class StaticBus
    {
        IMessageMapper messageMapper;
        ReadOnlySettings settings;

        public StaticBus(IMessageMapper messageMapper, ReadOnlySettings settings)
        {
            this.messageMapper = messageMapper;
            this.settings = settings;
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.PublishAsync"/>
        /// </summary>
        public Task PublishAsync<T>(Action<T> messageConstructor, NServiceBus.PublishOptions options, BehaviorContext context)
        {
            return PublishAsync(messageMapper.CreateInstance(messageConstructor), options, context);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.PublishAsync"/>
        /// </summary>
        public Task PublishAsync(object message, NServiceBus.PublishOptions options, BehaviorContext context)
        {
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
        /// <see cref="IBus.SubscribeAsync"/>
        /// </summary>
        public Task SubscribeAsync(Type eventType, SubscribeOptions options, BehaviorContext context)
        {
            var pipeline = new PipelineBase<SubscribeContext>(context.Builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var subscribeContext = new SubscribeContext(
                context,
                eventType,
                options);

            return pipeline.Invoke(subscribeContext);
        }

        /// <summary>
        /// <see cref="IBus.UnsubscribeAsync"/>
        /// </summary>
        public Task UnsubscribeAsync(Type eventType, UnsubscribeOptions options, BehaviorContext context)
        {
            var pipeline = new PipelineBase<UnsubscribeContext>(context.Builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var subscribeContext = new UnsubscribeContext(
                context,
                eventType,
                options);

            return pipeline.Invoke(subscribeContext);
        }

        public Task SendAsync<T>(Action<T> messageConstructor, NServiceBus.SendOptions options, BehaviorContext context)
        {
            return SendAsync(messageMapper.CreateInstance(messageConstructor), options, context);
        }

        public Task SendAsync(object message, NServiceBus.SendOptions options, BehaviorContext context)
        {
            var messageType = message.GetType();

            return SendMessage(messageType, message, options, context);
        }

        Task SendMessage(Type messageType, object message, NServiceBus.SendOptions options, BehaviorContext context)
        {
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
        /// <param name="context">The context of the incoming message</param>
        public Task ReplyAsync(object message, NServiceBus.ReplyOptions options, BehaviorContext context)
        {
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

        /// <summary>
        /// Instantiates a message of type T and performs a regular <see cref="ReplyAsync"/>.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">Options for this reply.</param>
        /// <param name="context">The context of the incoming message</param>
        public Task ReplyAsync<T>(Action<T> messageConstructor, NServiceBus.ReplyOptions options, BehaviorContext context)
        {
            return ReplyAsync(messageMapper.CreateInstance(messageConstructor), options, context);
        }

        /// <summary>
        /// Forwards the current message being handled to the destination maintaining
        /// all of its transport-level properties and headers.
        /// </summary>
        public async Task ForwardCurrentMessageToAsync(string destination, IncomingContext context)
        {
            var messageBeingProcessed = context.Get<IncomingMessage>();

            var pipeline = new PipelineBase<RoutingContext>(
                context.Builder,
                settings,
                settings.Get<PipelineConfiguration>().MainPipeline);

            var outgoingMessage = new OutgoingMessage(
                messageBeingProcessed.MessageId, 
                messageBeingProcessed.Headers, 
                messageBeingProcessed.Body);

            var routingContext = new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(destination), context);

            await pipeline.Invoke(routingContext).ConfigureAwait(false);
        }

        /// <summary>
        /// Moves the message being handled to the back of the list of available 
        /// messages so it can be handled later.
        /// </summary>
        public async Task HandleCurrentMessageLaterAsync(InvokeHandlerContext context)
        {
            if (context.handleCurrentMessageLaterWasCalled)
            {
                return;
            }

            var messageBeingProcessed = context.Get<IncomingMessage>();

            var pipeline = new PipelineBase<RoutingContext>(
                context.Builder, 
                settings, 
                settings.Get<PipelineConfiguration>().MainPipeline);

            var outgoingMessage = new OutgoingMessage(
                messageBeingProcessed.MessageId, 
                messageBeingProcessed.Headers, 
                messageBeingProcessed.Body);

            var routingContext = new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(settings.LocalAddress()), context);

            await pipeline.Invoke(routingContext).ConfigureAwait(false);

            context.handleCurrentMessageLaterWasCalled = true;
            context.DoNotInvokeAnyMoreHandlers();
        }
    }
}