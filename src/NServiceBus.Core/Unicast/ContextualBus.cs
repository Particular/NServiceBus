namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Janitor;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DeliveryConstraints;
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

            //if we're a worker, send to the distributor data bus
            if (settings.GetOrDefault<bool>("Worker.Enabled"))
            {
                sendLocalAddress = settings.Get<string>("MasterNode.Address");
            }
            else
            {
                sendLocalAddress = settings.LocalAddress();
            }
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Publish"/>
        /// </summary>
        public Task Publish<T>(Action<T> messageConstructor, NServiceBus.PublishOptions options)
        {
            return Publish(messageMapper.CreateInstance(messageConstructor), options);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Publish"/>
        /// </summary>
        public Task Publish(object message, NServiceBus.PublishOptions options)
        {
            var pipeline = new PipelineBase<OutgoingPublishContext>(builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);
         
            var publishContext = new OutgoingPublishContext(
                incomingContext,
                new OutgoingLogicalMessage(message),
                options);
            return outgoingPipeline.Invoke(outgoingContext);

            pipeline.Invoke(publishContext);
        }


        public void Subscribe(Type eventType, SubscribeOptions options)
        {
            var pipeline = new PipelineBase<SubscribeContext>(builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var subscribeContext = new SubscribeContext(
                incomingContext,
                eventType,
                options);

            pipeline.Invoke(subscribeContext);   
        }

        public void Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            var pipeline = new PipelineBase<UnsubscribeContext>(builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var subscribeContext = new UnsubscribeContext(
                incomingContext,
                eventType,
                options);

            pipeline.Invoke(subscribeContext);
        }

        /// <summary>
        /// <see cref="IBus.Reply"/>
        /// </summary>
        public Task Reply<T>(Action<T> messageConstructor, NServiceBus.ReplyOptions options)
        {
            return Reply(messageMapper.CreateInstance(messageConstructor), options);
        }

        /// <summary>
        /// <see cref="IBus.Reply"/>
        /// </summary>
        public Task Reply(object message, NServiceBus.ReplyOptions options)
        {
            var pipeline = new PipelineBase<OutgoingReplyContext>(builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var outgoingContext = new OutgoingReplyContext(
                incomingContext,
                new OutgoingLogicalMessage(message),
                options);

            return pipeline.Invoke(outgoingContext);
        }

        /// <summary>
        /// <see cref="IBus.HandleCurrentMessageLater"/>
        /// </summary>
        public async Task HandleCurrentMessageLater()
        {
            if (incomingContext.handleCurrentMessageLaterWasCalled)
            {
                return;
            }

            await dispatcher.Dispatch(new OutgoingMessage(MessageBeingProcessed.Id, MessageBeingProcessed.Headers, MessageBeingProcessed.Body), new DispatchOptions(sendLocalAddress, new AtomicWithReceiveOperation(), new List<DeliveryConstraint>(), new ContextBag()))
                .ConfigureAwait(false);

            incomingContext.handleCurrentMessageLaterWasCalled = true;

            ((HandlingStageBehavior.Context)incomingContext).DoNotInvokeAnyMoreHandlers();
        }

        /// <summary>
        /// <see cref="IBus.ForwardCurrentMessageTo(string)"/>
        /// </summary>
        public Task ForwardCurrentMessageTo(string destination)
        {
            return dispatcher.Dispatch(new OutgoingMessage(MessageBeingProcessed.Id, MessageBeingProcessed.Headers, MessageBeingProcessed.Body), new DispatchOptions(destination, new AtomicWithReceiveOperation(), new List<DeliveryConstraint>(), new ContextBag()));
        }

        public Task Send<T>(Action<T> messageConstructor, NServiceBus.SendOptions options)
        {
            return Send(messageMapper.CreateInstance(messageConstructor), options);
        }

        public Task Send(object message, NServiceBus.SendOptions options)
        {
            var messageType = message.GetType();

            return SendMessage(messageType, message, options);
        }

        void SendMessage(Type messageType, object message, NServiceBus.SendOptions options)
        {
            var pipeline = new PipelineBase<OutgoingSendContext>(builder, settings, settings.Get<PipelineConfiguration>().MainPipeline);

            var outgoingContext = new OutgoingSendContext(
                incomingContext,
                new OutgoingLogicalMessage(messageType, message),
                options);
            return outgoingPipeline.Invoke(outgoingContext);

            pipeline.Invoke(outgoingContext);
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
        readonly BehaviorContextStacker contextStacker;
        IBuilder builder;
        IDispatchMessages dispatcher;
        string sendLocalAddress;
        ReadOnlySettings settings;
    }
}