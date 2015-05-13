namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Janitor;
    using NServiceBus.Extensibility;
    using NServiceBus.Hosting;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.MessagingBestPractices;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Settings;
    using NServiceBus.Support;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Messages;
    using NServiceBus.Unicast.Routing;

    interface IContextualBus
    {
    }

    [SkipWeaving]
    partial class ContextualBus : IBus, IContextualBus
    {
        readonly IMessageMapper messageMapper;
        readonly Func<BehaviorContext> contextGetter;
        readonly IBuilder builder;
        readonly Configure configure;
        readonly IManageSubscriptions subscriptionManager;
        readonly MessageMetadataRegistry messageMetadataRegistry;
        readonly TransportDefinition transportDefinition;
        readonly ISendMessages messageSender;
        readonly StaticMessageRouter messageRouter;
        readonly HostInformation hostInformation;
        readonly PipelineBase<OutgoingContext> outgoingPipeline;
        readonly bool sendOnlyMode;
        readonly string sendLocalAddress;
        readonly string endpointName;


        public ContextualBus(Func<BehaviorContext> contextGetter, IMessageMapper messageMapper, IBuilder builder, Configure configure, IManageSubscriptions subscriptionManager,
            MessageMetadataRegistry messageMetadataRegistry, ReadOnlySettings settings, TransportDefinition transportDefinition, ISendMessages messageSender, StaticMessageRouter messageRouter, HostInformation hostInformation)
        {
            this.messageMapper = messageMapper;
            this.contextGetter = contextGetter;
            this.builder = builder;
            this.configure = configure;
            this.subscriptionManager = subscriptionManager;
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.transportDefinition = transportDefinition;
            this.messageSender = messageSender;
            this.messageRouter = messageRouter;
            this.hostInformation = hostInformation;
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

            endpointName = settings.EndpointName();
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

            var deliveryOptions = new DeliveryMessageOptions();

            ApplyDefaultDeliveryOptionsIfNeeded(deliveryOptions, messageType);

            var headers = new Dictionary<string, string>();

            ApplyStaticHeaders(headers);
            ApplyReplyToAddress(headers);
            ApplyHostRelatedHeaders(headers);

            var outgoingContext = new OutgoingContext(
                incomingContext,
                deliveryOptions,
                CombGuid.Generate().ToString(),
                MessageIntentEnum.Publish,
                messageType,
                message,
                options.Extensions);


            //todo: just for now
            foreach (var header in headers)
            {
                outgoingContext.SetHeader(header.Key, header.Value);
            }
            outgoingPipeline.Invoke(outgoingContext);
        }

        void ApplyStaticHeaders(Dictionary<string, string> messageHeaders)
        {
            foreach (var staticHeader in configure.OutgoingHeaders)
            {
                messageHeaders[staticHeader.Key] = staticHeader.Value;
            }
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
            var correlationId = !string.IsNullOrEmpty(MessageBeingProcessed.CorrelationId) ? MessageBeingProcessed.CorrelationId : MessageBeingProcessed.Id;

            var sendOptions = new SendMessageOptions(MessageBeingProcessed.ReplyToAddress);

            SendMessage(null, correlationId, MessageIntentEnum.Reply, sendOptions, message.GetType(), message, options.Extensions);
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

            messageSender.Send(new OutgoingMessage(MessageBeingProcessed.Id, MessageBeingProcessed.Headers, MessageBeingProcessed.Body), new TransportSendOptions(sendLocalAddress));

            incomingContext.handleCurrentMessageLaterWasCalled = true;

            ((HandlingStageBehavior.Context)incomingContext).DoNotInvokeAnyMoreHandlers();
        }

        /// <summary>
        /// <see cref="IBus.ForwardCurrentMessageTo(string)"/>
        /// </summary>
        public void ForwardCurrentMessageTo(string destination)
        {
            messageSender.Send(new OutgoingMessage(MessageBeingProcessed.Id, MessageBeingProcessed.Headers, MessageBeingProcessed.Body), new TransportSendOptions(destination));
        }

        public void Send<T>(Action<T> messageConstructor, NServiceBus.SendOptions options)
        {
            Send(messageMapper.CreateInstance(messageConstructor), options);
        }

        public void Send(object message, NServiceBus.SendOptions options)
        {
            var messageType = message.GetType();
            var destination = options.Destination;

            if (string.IsNullOrEmpty(destination))
            {
                destination = GetDestinationForSend(messageType);
            }

            TimeSpan? delayDeliveryFor = null;
            if (options.Delay.HasValue)
            {
                delayDeliveryFor = options.Delay;
            }

            DateTime? deliverAt = null;
            if (options.At.HasValue)
            {
                deliverAt = options.At;
            }

            var sendOptions = new SendMessageOptions(destination, deliverAt, delayDeliveryFor);

            SendMessage(options.MessageId, options.CorrelationId, options.Intent, sendOptions, messageType, message, options.Extensions);
        }

        public void SendLocal<T>(Action<T> messageConstructor, SendLocalOptions options)
        {
            SendLocal(messageMapper.CreateInstance(messageConstructor), options);
        }

        public void SendLocal(object message, SendLocalOptions options)
        {
            var destination = sendLocalAddress;

            TimeSpan? delayDeliveryFor = null;
            if (options.Delay.HasValue)
            {
                delayDeliveryFor = options.Delay;
            }

            DateTime? deliverAt = null;
            if (options.At.HasValue)
            {
                deliverAt = options.At;
            }

            var sendOptions = new SendMessageOptions(destination, deliverAt, delayDeliveryFor);

            SendMessage(options.MessageId, options.CorrelationId, MessageIntentEnum.Send, sendOptions, message.GetType(), message, options.Extensions);
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

        string GetDestinationForSend(Type messageType)
        {
            var destinations = GetAtLeastOneAddressForMessageType(messageType);

            if (destinations.Count > 1)
            {
                throw new InvalidOperationException("Sends can only target one address.");
            }

            return destinations.SingleOrDefault();
        }

      

        void SendMessage(string messageId, string correlationId, MessageIntentEnum intent, SendMessageOptions sendOptions, Type messageType, object message, OptionExtensionContext context)
        {
            var headers = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(messageId))
            {
                messageId = CombGuid.Generate().ToString();
            }

            if (!string.IsNullOrEmpty(correlationId))
            {
                headers[Headers.CorrelationId] = correlationId;
            }
            else
            {
                headers[Headers.CorrelationId] = messageId;
            }

            ApplyReplyToAddress(headers);

            ApplyDefaultDeliveryOptionsIfNeeded(sendOptions, messageType);

            ApplyStaticHeaders(headers);

            ApplyHostRelatedHeaders(headers);

            

            var outgoingContext = new OutgoingContext(
                incomingContext,
                sendOptions,
                messageId,
                intent,
                messageType,
                message,
                context);

            //todo: just for now
            foreach (var header in headers)
            {
                outgoingContext.SetHeader(header.Key,header.Value);
            }

            outgoingPipeline.Invoke(outgoingContext);
        }

        void ApplyHostRelatedHeaders(Dictionary<string, string> headers)
        {
            headers.Add(Headers.OriginatingMachine, RuntimeEnvironment.MachineName);
            headers.Add(Headers.OriginatingEndpoint, endpointName);
            headers.Add(Headers.OriginatingHostId, hostInformation.HostId.ToString("N"));
        }

        private void ApplyReplyToAddress(Dictionary<string, string> headers)
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

                if (!incomingContext.TryGet(TransportReceiveContext.IncomingPhysicalMessageKey, out current))
                {
                    return null;
                }

                return new MessageContext(current);
            }
        }


        void ApplyDefaultDeliveryOptionsIfNeeded(DeliveryMessageOptions options, Type messageType)
        {
            var messageDefinitions = messageMetadataRegistry.GetMessageMetadata(messageType);

            if (!options.TimeToBeReceived.HasValue)
            {
                if (messageDefinitions.TimeToBeReceived < TimeSpan.MaxValue)
                {
                    options.TimeToBeReceived = messageDefinitions.TimeToBeReceived;
                }
            }

            if (!options.NonDurable.HasValue)
            {
                options.NonDurable = !messageDefinitions.Recoverable;
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

                if (!incomingContext.TryGet(TransportReceiveContext.IncomingPhysicalMessageKey, out current))
                {
                    throw new InvalidOperationException("There is no current message being processed");
                }

                return current;
            }
        }
    }
}