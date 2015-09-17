namespace NServiceBus.Unicast
{
    using System;
    using System.Threading.Tasks;
    using Janitor;
    using NServiceBus.Extensibility;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    [SkipWeaving]
    partial class ContextualBus : IBus, IContextualBus
    {
        public ContextualBus(BehaviorContextStacker contextStacker, IMessageMapper messageMapper, IBuilder builder,
            ReadOnlySettings settings,IDispatchMessages dispatcher)
        {
            this.messageMapper = messageMapper;
            this.contextStacker = contextStacker;
            this.builder = builder;
            this.dispatcher = dispatcher;
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
                incomingContext,
                new OutgoingLogicalMessage(message),
                options);

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
                incomingContext,
                new OutgoingLogicalMessage(message),
                options);

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

            await dispatcher.Dispatch(new OutgoingMessage(MessageBeingProcessed.Id, MessageBeingProcessed.Headers, MessageBeingProcessed.Body), new DispatchOptions(new DirectToTargetDestination(sendLocalAddress), new ContextBag())).ConfigureAwait(false);

            incomingContext.handleCurrentMessageLaterWasCalled = true;

            ((HandlingStageBehavior.Context)incomingContext).DoNotInvokeAnyMoreHandlers();
        }

        /// <summary>
        /// <see cref="IBus.ForwardCurrentMessageToAsync"/>
        /// </summary>
        public Task ForwardCurrentMessageToAsync(string destination)
        {
            return dispatcher.Dispatch(new OutgoingMessage(MessageBeingProcessed.Id, MessageBeingProcessed.Headers, MessageBeingProcessed.Body), new DispatchOptions(new DirectToTargetDestination(destination), new ContextBag()));
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
                incomingContext,
                new OutgoingLogicalMessage(messageType, message),
                options);

            return pipeline.Invoke(outgoingContext);
        }

        /// <summary>
        /// <see cref="IBus.DoNotContinueDispatchingCurrentMessageToHandlers"/>
        /// </summary>
        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            ((HandlingStageBehavior.Context)incomingContext).DoNotInvokeAnyMoreHandlers();
        }

        /// <summary>
        /// <see cref="IBus.CurrentMessageContext"/>.
        /// </summary>
        public IMessageContext CurrentMessageContext
        {
            get
            {
                TransportMessage current;

                if (!incomingContext.TryGet(out current))
                {
                    return null;
                }

                return new MessageContext(current);
            }
        }

        BehaviorContext incomingContext
        {
            get { return contextStacker.GetCurrentOrRootContext(); }
        }

        TransportMessage MessageBeingProcessed
        {
            get
            {
                TransportMessage current;

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
        IDispatchMessages dispatcher;
        string sendLocalAddress;
        ReadOnlySettings settings;
    }
}