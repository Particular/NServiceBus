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

    class BusOperations
    {
        IMessageMapper messageMapper;
        ReadOnlySettings settings;

        public BusOperations(IMessageMapper messageMapper, ReadOnlySettings settings)
        {
            this.messageMapper = messageMapper;
            this.settings = settings;
        }

        public Task PublishAsync<T>(Action<T> messageConstructor, NServiceBus.PublishOptions options, BehaviorContext context)
        {
            return PublishAsync(messageMapper.CreateInstance(messageConstructor), options, context);
        }

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

        public Task SubscribeAsync(Type eventType, SubscribeOptions options, BehaviorContext context)
        {
            var pipeline = new PipelineBase<SubscribeContext>(context.Builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var subscribeContext = new SubscribeContext(
                context,
                eventType,
                options);

            return pipeline.Invoke(subscribeContext);
        }

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

        public Task ReplyAsync<T>(Action<T> messageConstructor, NServiceBus.ReplyOptions options, BehaviorContext context)
        {
            return ReplyAsync(messageMapper.CreateInstance(messageConstructor), options, context);
        }

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