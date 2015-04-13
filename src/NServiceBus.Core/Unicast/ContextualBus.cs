namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Janitor;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Settings;
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
        readonly LogicalMessageFactory messageFactory;
        readonly TransportDefinition transportDefinition;
        readonly ISendMessages messageSender;
        readonly StaticMessageRouter messageRouter;
        readonly CallbackMessageLookup callbackMessageLookup;
        readonly PipelineBase<OutgoingContext> outgoingPipeline;
        readonly bool sendOnlyMode;
        readonly string sendLocalAddress;

        public ContextualBus(Func<BehaviorContext> contextGetter, IMessageMapper messageMapper, IBuilder builder, Configure configure, IManageSubscriptions subscriptionManager,
            MessageMetadataRegistry messageMetadataRegistry, ReadOnlySettings settings, TransportDefinition transportDefinition, ISendMessages messageSender, StaticMessageRouter messageRouter, CallbackMessageLookup callbackMessageLookup)
        {
            this.messageMapper = messageMapper;
            this.contextGetter = contextGetter;
            this.builder = builder;
            this.configure = configure;
            this.subscriptionManager = subscriptionManager;
            this.messageMetadataRegistry = messageMetadataRegistry;
            messageFactory = new LogicalMessageFactory(messageMetadataRegistry, messageMapper);
            this.transportDefinition = transportDefinition;
            this.messageSender = messageSender;
            this.messageRouter = messageRouter;
            this.callbackMessageLookup = callbackMessageLookup;
            outgoingPipeline = new PipelineBase<OutgoingContext>(builder, settings.Get<PipelineModifications>());
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
        /// <see cref="ISendOnlyBus.Publish{T}(Action{T})"/>
        /// </summary>
        public void Publish<T>(Action<T> messageConstructor)
        {
            Publish(messageMapper.CreateInstance(messageConstructor));
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Publish{T}()"/>
        /// </summary>
        public virtual void Publish<T>()
        {
            Publish(messageMapper.CreateInstance<T>());
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Publish(object)"/>
        /// </summary>
        public virtual void Publish(object message)
        {
            var logicalMessage = messageFactory.Create(message);

            var options = new PublishMessageOptions(logicalMessage.MessageType);

            ApplyDefaultDeliveryOptionsIfNeeded(options, logicalMessage);

            var headers = new Dictionary<string, string>();

            ApplyStaticHeaders(headers);
            ApplyReplyToAddress(headers);


            var outgoingContext = new OutgoingContext(
                incomingContext,
                options,
                logicalMessage,
                headers,
                CombGuid.Generate().ToString(),
                MessageIntentEnum.Publish);


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

        bool SendOnlyMode { get { return sendOnlyMode; } }

        /// <summary>
        /// <see cref="IBus.Subscribe(Type)"/>
        /// </summary>
        public virtual void Subscribe(Type messageType)
        {
            MessagingBestPractices.AssertIsValidForPubSub(messageType, builder.Build<Conventions>());

            if (SendOnlyMode)
            {
                throw new InvalidOperationException("It's not allowed for a send only endpoint to be a subscriber");
            }

            AssertHasLocalAddress();

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

        void AssertHasLocalAddress()
        {
            if (configure.LocalAddress == null)
            {
                throw new InvalidOperationException("Cannot start subscriber without a queue configured. Please specify the LocalAddress property of UnicastBusConfig.");
            }
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
            MessagingBestPractices.AssertIsValidForPubSub(messageType, builder.Build<Conventions>());

            if (SendOnlyMode)
            {
                throw new InvalidOperationException("It's not allowed for a send only endpoint to unsubscribe");
            }

            AssertHasLocalAddress();

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

        /// <summary>
        /// <see cref="IBus.Reply(object)"/>
        /// </summary>
        public void Reply(object message)
        {
            var context = new NServiceBus.SendOptions(correlationId: GetCorrelationId());

            context.AsReplyTo(MessageBeingProcessed.ReplyToAddress);

            Send(message, context);
        }

        /// <summary>
        /// <see cref="IBus.Reply{T}(Action{T})"/>
        /// </summary>
        public void Reply<T>(Action<T> messageConstructor)
        {
            var context = new NServiceBus.SendOptions(correlationId: GetCorrelationId());
            context.AsReplyTo(MessageBeingProcessed.ReplyToAddress);

            Send(messageConstructor, context);
        }

        /// <summary>
        /// <see cref="IBus.Return{T}"/>
        /// </summary>
        public void Return<T>(T errorCode)
        {
            var tType = errorCode.GetType();
            if (!(tType.IsEnum || tType == typeof(Int32) || tType == typeof(Int16) || tType == typeof(Int64)))
            {
                throw new ArgumentException("The errorCode can only be an enum or an integer.", "errorCode");
            }

            var returnCode = errorCode.ToString();
            if (tType.IsEnum)
            {
                returnCode = Enum.Format(tType, errorCode, "D");
            }


            var context = new NServiceBus.SendOptions(correlationId: GetCorrelationId());

            context.AddHeader(Headers.ReturnMessageErrorCodeHeader, returnCode);
            context.AsReplyTo(MessageBeingProcessed.ReplyToAddress);

            SendMessage(context, new ControlMessage("Bus.Return(" + returnCode + ")"));
        }

        string GetCorrelationId()
        {
            return !string.IsNullOrEmpty(MessageBeingProcessed.CorrelationId) ? MessageBeingProcessed.CorrelationId : MessageBeingProcessed.Id;
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

        public ICallback Send<T>(Action<T> messageConstructor, NServiceBus.SendOptions options)
        {
            return Send(messageMapper.CreateInstance(messageConstructor), options);
        }

        public ICallback Send(object message, NServiceBus.SendOptions options)
        {
            return SendMessage(options, messageFactory.Create(message));
        }

        public ICallback SendLocal<T>(Action<T> messageConstructor, SendLocalOptions options)
        {
            return SendLocal(messageMapper.CreateInstance(messageConstructor), options);
        }

        public ICallback SendLocal(object message, SendLocalOptions options)
        {
            return SendMessage(options, messageFactory.Create(message));
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

        ICallback SendMessage(SendLocalOptions options, LogicalMessage message)
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

            var sendOptions = new SendMessageOptions(destination, deliverAt, delayDeliveryFor, options.Context);

            return SendMessage(options.MessageId, options.CorrelationId, MessageIntentEnum.Send, options.Headers, sendOptions, message);
        }

        ICallback SendMessage(NServiceBus.SendOptions options, LogicalMessage message)
        {
            var destination = options.Destination;

            if (string.IsNullOrEmpty(destination))
            {
                destination = GetDestinationForSend(message.MessageType);
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

            var sendOptions = new SendMessageOptions(destination, deliverAt, delayDeliveryFor, options.Context);

            return SendMessage(options.MessageId, options.CorrelationId, options.Intent, options.Headers, sendOptions, message);
        }

        ICallback SendMessage(string messageId, string correlationId, MessageIntentEnum intent, Dictionary<string, string> messageHeaders, SendMessageOptions sendOptions, LogicalMessage message)
        {
            var headers = new Dictionary<string, string>(messageHeaders);

            headers[Headers.MessageIntent] = intent.ToString();

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

            ApplyDefaultDeliveryOptionsIfNeeded(sendOptions, message);

            ApplyStaticHeaders(headers);

            var outgoingContext = new OutgoingContext(
                incomingContext,
                sendOptions,
                message,
                headers,
                messageId,
                intent);

            outgoingPipeline.Invoke(outgoingContext);

            return SetupCallback(messageId);
        }

        private void ApplyReplyToAddress(Dictionary<string, string> headers)
        {
            string replyToAddress = null;

            if (!SendOnlyMode)
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

        ICallback SetupCallback(string transportMessageId)
        {
            var result = new Callback(transportMessageId, sendOnlyMode);
            result.Registered += (sender, args) => callbackMessageLookup.RegisterResult(args.MessageId, args.TaskCompletionSource);

            return result;
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


        void ApplyDefaultDeliveryOptionsIfNeeded(DeliveryMessageOptions options, LogicalMessage logicalMessage)
        {
            if (logicalMessage is ControlMessage)
            {
                return;
            }

            var messageDefinitions = messageMetadataRegistry.GetMessageMetadata(logicalMessage.MessageType);

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