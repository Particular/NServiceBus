namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Janitor;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Extensibility;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.MessagingBestPractices;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Settings;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Routing;

    interface IContextualBus
    {
    }

    [SkipWeaving]
    partial class ContextualBus : IBus, IContextualBus
    {
        IMessageMapper messageMapper;
        Func<BehaviorContext> contextGetter;
        IBuilder builder;
        Configure configure;
        IManageSubscriptions subscriptionManager;
        TransportDefinition transportDefinition;
        IDispatchMessages messageSender;
        StaticMessageRouter messageRouter;
        PipelineBase<OutgoingContext> outgoingPipeline;
        bool sendOnlyMode;
        string sendLocalAddress;
      

        public ContextualBus(Func<BehaviorContext> contextGetter, IMessageMapper messageMapper, IBuilder builder, Configure configure, IManageSubscriptions subscriptionManager,
            ReadOnlySettings settings, TransportDefinition transportDefinition, IDispatchMessages messageSender, StaticMessageRouter messageRouter)
        {
            this.messageMapper = messageMapper;
            this.contextGetter = contextGetter;
            this.builder = builder;
            this.configure = configure;
            this.subscriptionManager = subscriptionManager;
            this.transportDefinition = transportDefinition;
            this.messageSender = messageSender;
            this.messageRouter = messageRouter;
            var pipelinesCollection = settings.Get<PipelineConfiguration>();
            outgoingPipeline = new PipelineBase<OutgoingContext>(builder,  settings, pipelinesCollection.MainPipeline);
            sendOnlyMode = settings.Get<bool>("Endpoint.SendOnly");
            //if we're a worker, send to the distributor data bus
            if (settings.GetOrDefault<bool>("Worker.Enabled"))
            {
                sendLocalAddress = settings.Get<string>("MasterNode.Address");
            }
            else
            {
                sendLocalAddress = configure.LocalAddress;
            }
        }

        BehaviorContext incomingContext
        {
            get { return contextGetter(); }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            //Injected
        }

        /// <summary>
        /// Sets whether or not the return address of a received message
        /// should be propagated when the message is forwarded. This field is
        /// used primarily for the Distributor.
        /// </summary>
        public bool PropagateReturnAddressOnSend { get; set; }

        /// <summary>
        /// <see cref="ISendOnlyBus.Publish"/>
        /// </summary>
        public void Publish<T>(Action<T> messageConstructor, NServiceBus.PublishOptions options)
        {
            Publish(messageMapper.CreateInstance(messageConstructor), options);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Publish"/>
        /// </summary>
        public void Publish(object message, NServiceBus.PublishOptions options)
        {
            var messageType = message.GetType();

            var headers = new Dictionary<string, string>();

            ApplyReplyToAddress(headers);
        
            var outgoingContext = new OutgoingContext(
                incomingContext,
                messageType,
                message,
                options);


            foreach (var header in headers)
            {
                outgoingContext.SetHeader(header.Key, header.Value);
            }
       
            outgoingPipeline.Invoke(outgoingContext);
        }

        /// <summary>
        /// <see cref="IBus.Subscribe{T}()"/>
        /// </summary>
        public void Subscribe<T>()
        {
            Subscribe(typeof(T));
        }

        /// <summary>
        /// <see cref="IBus.Subscribe(Type)"/>
        /// </summary>
        public virtual void Subscribe(Type messageType)
        {
            AssertIsValidForPubSub(messageType);
         
            if (transportDefinition.HasSupportForCentralizedPubSub)
            {
                // We are dealing with a brokered transport wired for auto pub/sub.
                subscriptionManager.Subscribe(messageType, null);
                return;
            }

            var addresses = GetAtLeastOneAddressForMessageType(messageType);

            foreach (var destination in addresses)
            {
                subscriptionManager.Subscribe(messageType, destination);
            }
        }

        /// <summary>
        /// <see cref="IBus.Unsubscribe{T}()"/>
        /// </summary>
        public void Unsubscribe<T>()
        {
            Unsubscribe(typeof(T));
        }

        /// <summary>
        /// <see cref="IBus.Unsubscribe(Type)"/>
        /// </summary>
        public virtual void Unsubscribe(Type messageType)
        {
            AssertIsValidForPubSub(messageType);

            if (transportDefinition.HasSupportForCentralizedPubSub)
            {
                // We are dealing with a brokered transport wired for auto pub/sub.
                subscriptionManager.Unsubscribe(messageType, null);
                return;
            }

            var addresses = GetAtLeastOneAddressForMessageType(messageType);

            foreach (var destination in addresses)
            {
                subscriptionManager.Unsubscribe(messageType, destination);
            }
        }

        void AssertIsValidForPubSub(Type messageType)
        {
            //we don't have any extension points for subscribe/unsubscribe but this does the trick for now
            if (configure.container.HasComponent<Validations>())
            {
                builder.Build<Validations>()
                    .AssertIsValidForPubSub(messageType);
            }
        }

        /// <summary>
        /// <see cref="IBus.Reply"/>
        /// </summary>
        public void Reply<T>(Action<T> messageConstructor, NServiceBus.ReplyOptions options)
        {
            Reply(messageMapper.CreateInstance(messageConstructor), options);
        }

        /// <summary>
        /// <see cref="IBus.Reply"/>
        /// </summary>
        public void Reply(object message,NServiceBus.ReplyOptions options)
        {
            SendMessage(message.GetType(), message, options);
        }

        /// <summary>
        /// <see cref="IBus.HandleCurrentMessageLater"/>
        /// </summary>
        public void HandleCurrentMessageLater()
        {
            if (incomingContext.handleCurrentMessageLaterWasCalled)
            {
                return;
            }

            messageSender.Dispatch(new OutgoingMessage(MessageBeingProcessed.Id, MessageBeingProcessed.Headers, MessageBeingProcessed.Body), new DispatchOptions(sendLocalAddress,new AtomicWithReceiveOperation(), new List<DeliveryConstraint>()));

            incomingContext.handleCurrentMessageLaterWasCalled = true;

            ((HandlingStageBehavior.Context)incomingContext).DoNotInvokeAnyMoreHandlers();
        }

        /// <summary>
        /// <see cref="IBus.ForwardCurrentMessageTo(string)"/>
        /// </summary>
        public void ForwardCurrentMessageTo(string destination)
        {
            messageSender.Dispatch(new OutgoingMessage(MessageBeingProcessed.Id, MessageBeingProcessed.Headers, MessageBeingProcessed.Body), new DispatchOptions(destination, new AtomicWithReceiveOperation(), new List<DeliveryConstraint>()));
        }

        public void Send<T>(Action<T> messageConstructor, NServiceBus.SendOptions options)
        {
            Send(messageMapper.CreateInstance(messageConstructor), options);
        }

        public void Send(object message, NServiceBus.SendOptions options)
        {
            var messageType = message.GetType();
   
            SendMessage(messageType, message, options);
        }

        public void SendLocal<T>(Action<T> messageConstructor, SendLocalOptions options)
        {
            SendLocal(messageMapper.CreateInstance(messageConstructor), options);
        }

        public void SendLocal(object message, SendLocalOptions options)
        {
            var sendOptions = new NServiceBus.SendOptions
            {
                Extensions = options.Extensions
            };


            sendOptions.RouteToLocalEndpointInstance();

            Send(message, sendOptions);
        }

        List<string> GetAtLeastOneAddressForMessageType(Type messageType)
        {
            var addresses = GetAddressForMessageType(messageType)
                .Distinct()
                .ToList();
            if (addresses.Count == 0)
            {
                var error = string.Format("No destination could be found for message type {0}. Check the <MessageEndpointMappings> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.", messageType);
                throw new InvalidOperationException(error);
            }
            return addresses;
        }

      
        void SendMessage(Type messageType, object message, ExtendableOptions options)
        {
            var headers = new Dictionary<string, string>();

         
            //todo: move to routing
            ApplyReplyToAddress(headers);
            
            var outgoingContext = new OutgoingContext(
                incomingContext,
                messageType,
                message,
                options);

            foreach (var header in headers)
            {
                outgoingContext.SetHeader(header.Key,header.Value);
            }

            outgoingPipeline.Invoke(outgoingContext);
        }

        void ApplyReplyToAddress(Dictionary<string, string> headers)
        {
            string replyToAddress = null;

            if (!sendOnlyMode)
            {
                replyToAddress = configure.PublicReturnAddress;
            }

            if (PropagateReturnAddressOnSend && CurrentMessageContext != null)
            {
                replyToAddress = CurrentMessageContext.ReplyToAddress;
            }
            if (!string.IsNullOrEmpty(replyToAddress))
            {
                headers[Headers.ReplyToAddress] = replyToAddress;
            }
        }

        /// <summary>
        /// <see cref="IBus.DoNotContinueDispatchingCurrentMessageToHandlers"/>
        /// </summary>
        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            ((HandlingStageBehavior.Context)incomingContext).DoNotInvokeAnyMoreHandlers();
        }

        /// <summary>
        /// <see cref="IBus.CurrentMessageContext"/>
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

        List<string> GetAddressForMessageType(Type messageType)
        {
            var destination = messageRouter.GetDestinationFor(messageType);

            if (destination.Any())
            {
                return destination;
            }

            if (messageMapper != null && !messageType.IsInterface)
            {
                var t = messageMapper.GetMappedTypeFor(messageType);
                if (t != null && t != messageType)
                {
                    return GetAddressForMessageType(t);
                }
            }

            return destination;
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
    }
}