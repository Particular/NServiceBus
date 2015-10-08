namespace NServiceBus.Unicast
{
    using System;
    using System.Threading.Tasks;
    using Janitor;
    using MessageInterfaces;
    using ObjectBuilder;
    using OutgoingPipeline;
    using Pipeline;
    using Pipeline.Contexts;
    using NServiceBus.Routing;
    using Settings;
    using TransportDispatch;
    using Transports;

    [SkipWeaving]
    partial class ContextualBus : IBus, IContextualBus
    {
        public ContextualBus(BehaviorContextStacker contextStacker, IMessageMapper messageMapper, IBuilder builder, ReadOnlySettings settings)
        {
            this.messageMapper = messageMapper;
            this.contextStacker = contextStacker;
            this.builder = builder;
            this.settings = settings;
            sendLocalAddress = settings.LocalAddress();
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.PublishAsync"/>
        /// </summary>
        public Task PublishAsync<T>(Action<T> messageConstructor, NServiceBus.PublishOptions options)
        {
            return PublishAsync(messageMapper.CreateInstance(messageConstructor), options);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.PublishAsync"/>
        /// </summary>
        public Task PublishAsync(object message, NServiceBus.PublishOptions options)
        {
            var pipeline = new PipelineBase<OutgoingPublishContext>(builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var publishContext = new OutgoingPublishContext(
                new OutgoingLogicalMessage(message),
                options,
                incomingContext);

            return pipeline.Invoke(publishContext);
        }

        /// <summary>
        /// <see cref="IBus.SubscribeAsync"/>
        /// </summary>
        public Task SubscribeAsync(Type eventType, SubscribeOptions options)
        {
            var pipeline = new PipelineBase<SubscribeContext>(builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var subscribeContext = new SubscribeContext(
                incomingContext,
                eventType,
                options);

            return pipeline.Invoke(subscribeContext);
        }

        /// <summary>
        /// <see cref="IBus.UnsubscribeAsync"/>
        /// </summary>
        public Task UnsubscribeAsync(Type eventType, UnsubscribeOptions options)
        {
            var pipeline = new PipelineBase<UnsubscribeContext>(builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var subscribeContext = new UnsubscribeContext(
                incomingContext,
                eventType,
                options);

            return pipeline.Invoke(subscribeContext);
        }

        /// <summary>
        /// <see cref="IBus.ReplyAsync"/>
        /// </summary>
        public Task ReplyAsync<T>(Action<T> messageConstructor, NServiceBus.ReplyOptions options)
        {
            return ReplyAsync(messageMapper.CreateInstance(messageConstructor), options);
        }

        /// <summary>
        /// <see cref="IBus.ReplyAsync"/>
        /// </summary>
        public Task ReplyAsync(object message, NServiceBus.ReplyOptions options)
        {
            var pipeline = new PipelineBase<OutgoingReplyContext>(builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var outgoingContext = new OutgoingReplyContext(
                new OutgoingLogicalMessage(message),
                options,
                incomingContext);

            return pipeline.Invoke(outgoingContext);
        }

        /// <summary>
        /// <see cref="IBus.HandleCurrentMessageLaterAsync"/>
        /// </summary>
        public async Task HandleCurrentMessageLaterAsync()
        {
            if (incomingContext.handleCurrentMessageLaterWasCalled)
            {
                return;
            }

            var pipeline = new PipelineBase<RoutingContext>(builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var outgoingMessage = new OutgoingMessage(MessageBeingProcessed.MessageId, MessageBeingProcessed.Headers, MessageBeingProcessed.Body);
            var context = new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(sendLocalAddress), incomingContext);

            await pipeline.Invoke(context);

            incomingContext.handleCurrentMessageLaterWasCalled = true;

            ((InvokeHandlerContext)incomingContext).DoNotInvokeAnyMoreHandlers();
        }

        /// <summary>
        /// <see cref="IBus.ForwardCurrentMessageToAsync"/>
        /// </summary>
        public async Task ForwardCurrentMessageToAsync(string destination)
        {
            var pipeline = new PipelineBase<RoutingContext>(builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var outgoingMessage = new OutgoingMessage(MessageBeingProcessed.MessageId, MessageBeingProcessed.Headers, MessageBeingProcessed.Body);
            var context = new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(destination), incomingContext);

            await pipeline.Invoke(context);
        }

        public Task SendAsync<T>(Action<T> messageConstructor, NServiceBus.SendOptions options)
        {
            return SendAsync(messageMapper.CreateInstance(messageConstructor), options);
        }

        public Task SendAsync(object message, NServiceBus.SendOptions options)
        {
            var messageType = message.GetType();

            return SendMessage(messageType, message, options);
        }

        Task SendMessage(Type messageType, object message, NServiceBus.SendOptions options)
        {
            var pipeline = new PipelineBase<OutgoingSendContext>(builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var outgoingContext = new OutgoingSendContext(
                new OutgoingLogicalMessage(messageType, message),
                options,
                incomingContext);

            return pipeline.Invoke(outgoingContext);
        }

        /// <summary>
        /// <see cref="IBus.DoNotContinueDispatchingCurrentMessageToHandlers"/>
        /// </summary>
        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            ((InvokeHandlerContext)incomingContext).DoNotInvokeAnyMoreHandlers();
        }

        /// <summary>
        /// <see cref="IBus.CurrentMessageContext"/>.
        /// </summary>
        public IMessageContext CurrentMessageContext
        {
            get
            {
                IncomingMessage current;

                if (!incomingContext.TryGet(out current))
                {
                    return null;
                }

                return new MessageContext(current);
            }
        }

        BehaviorContext incomingContext => contextStacker.GetCurrentOrRootContext();

        IncomingMessage MessageBeingProcessed
        {
            get
            {
                IncomingMessage current;

                if (!incomingContext.TryGet(out current))
                {
                    throw new InvalidOperationException("There is no current message being processed");
                }

                return current;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            //Injected
        }

        IMessageMapper messageMapper;
        BehaviorContextStacker contextStacker;
        IBuilder builder;
        string sendLocalAddress;
        ReadOnlySettings settings;
    }
}