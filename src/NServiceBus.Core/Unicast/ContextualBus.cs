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
    using NServiceBus.Unicast.Transport;

    interface IContextualBus
    {
    }

    [SkipWeaving]
    class ContextualBus : IBus, IContextualBus
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
        readonly PipelineBase<OutgoingContext> outgoing;
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
            outgoing = new PipelineBase<OutgoingContext>(builder, settings.Get<PipelineModifications>());
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

        BehaviorContext context
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

            var options = new PublishOptions(logicalMessage.MessageType);

            ApplyDefaultDeliveryOptionsIfNeeded(options, logicalMessage);

            var headers = new Dictionary<string, string>();
            ApplyStaticHeaders(headers);

            InvokeSendPipeline(options, logicalMessage, headers);
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
            var context = new SendContext
            {
                IsReply = true
            };

            context.SetDestination(MessageBeingProcessed.ReplyToAddress);
            context.SetCorrelationId(GetCorrelationId());
            Send(message, context);
        }

        /// <summary>
        /// <see cref="IBus.Reply{T}(Action{T})"/>
        /// </summary>
        public void Reply<T>(Action<T> messageConstructor)
        {
            var context = new SendContext
         {
             IsReply = true
         };

            context.SetDestination(MessageBeingProcessed.ReplyToAddress);
            context.SetCorrelationId(GetCorrelationId());
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


            var context = new SendContext
            {
                IsReply = true
            };

            context.SetHeader(Headers.ReturnMessageErrorCodeHeader, returnCode);
            context.SetDestination(MessageBeingProcessed.ReplyToAddress);
            context.SetCorrelationId(GetCorrelationId());

            SendMessage(context, new ControlMessage());
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
            if (context.handleCurrentMessageLaterWasCalled)
            {
                return;
            }

            messageSender.Send(new OutgoingMessage(MessageBeingProcessed.Headers, MessageBeingProcessed.Body), new SendOptions(sendLocalAddress));

            context.handleCurrentMessageLaterWasCalled = true;

            ((HandlingStageBehavior.Context)context).DoNotInvokeAnyMoreHandlers();
        }

        /// <summary>
        /// <see cref="IBus.ForwardCurrentMessageTo(string)"/>
        /// </summary>
        public void ForwardCurrentMessageTo(string destination)
        {
            messageSender.Send(new OutgoingMessage(MessageBeingProcessed.Headers, MessageBeingProcessed.Body), new SendOptions(destination));
        }

        /// <summary>
        /// <see cref="IBus.SendLocal{T}(Action{T})"/>
        /// </summary>
        public ICallback SendLocal<T>(Action<T> messageConstructor)
        {
            return SendLocal(messageMapper.CreateInstance(messageConstructor));
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send(object)"/>
        /// </summary>
        public ICallback SendLocal(object message)
        {
            var context = new SendContext();

            context.SetDestination(sendLocalAddress);

            return Send(message,context);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send{T}(Action{T})"/>
        /// </summary>
        public ICallback Send<T>(Action<T> messageConstructor)
        {

            return Send(messageConstructor, new SendContext());
        }

        public ICallback Send<T>(Action<T> messageConstructor, SendContext context)
        {
            object message = messageMapper.CreateInstance(messageConstructor);
            return SendMessage(context, messageFactory.Create(message));
        }



        /// <summary>
        /// <see cref="ISendOnlyBus.Send(object)"/>
        /// </summary>
        public ICallback Send(object message)
        {
            return Send(message, new SendContext());
        }


        public ICallback Send(object message, SendContext context)
        {
            return SendMessage(context, messageFactory.Create(message));
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

        /// <summary>
        /// <see cref="ISendOnlyBus.Send{T}(string,Action{T})"/>
        /// </summary>
        public ICallback Send<T>(string destination, Action<T> messageConstructor)
        {
            var context = new SendContext();

            context.SetDestination(destination);
            return Send(messageConstructor,context);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send(string,object)"/>
        /// </summary>
        public ICallback Send(string destination, object message)
        {
            var context = new SendContext();

            context.SetDestination(destination);

            return Send(message, context);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send{T}(string,string,Action{T})"/>
        /// </summary>
        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            var context = new SendContext();

            context.SetDestination(destination);
            context.SetCorrelationId(correlationId);

            return Send(messageConstructor, context);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send(string,string,object)"/>
        /// </summary>
        public ICallback Send(string destination, string correlationId, object message)
        {
            var context = new SendContext();

            context.SetDestination(destination);
            context.SetCorrelationId(correlationId);

            return Send(message,context);
        }

        /// <summary>
        /// <see cref="IBus.Defer(System.TimeSpan,object)"/>
        /// </summary>
        public ICallback Defer(TimeSpan delay, object message)
        {
            var context = new SendContext();

            context.DelayDeliveryWith(delay);

            return SendMessage(context, messageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="IBus.Defer(DateTime,object)"/>
        /// </summary>
        public ICallback Defer(DateTime processAt, object message)
        {
            var context = new SendContext();

            context.DeliverAt(processAt);

            return Send(message, context);
        }

        ICallback SendMessage(SendContext context, LogicalMessage message)
        {
            var destination = context.Destination;

            if (string.IsNullOrEmpty(destination))
            {
                GetDestinationForSend(message.MessageType);
            }

            var sendOptions = new SendOptions(destination);

            if (!string.IsNullOrEmpty(context.CorrelationId))
            {
                context.Headers[Headers.CorrelationId] = context.CorrelationId;
            }


            ApplyDefaultDeliveryOptionsIfNeeded(sendOptions, message);


            ApplyStaticHeaders(context.Headers);

            var physicalMessage = InvokeSendPipeline(sendOptions, message, context.Headers)
                .Get<TransportMessage>();

            return SetupCallback(physicalMessage.Id);
        }

        BehaviorContext InvokeSendPipeline(DeliveryOptions sendOptions, LogicalMessage message, Dictionary<string, string> headers)
        {

            SetReplyToAddressHeader(headers);


            var outgoingContext = new OutgoingContext(context, sendOptions, message, headers);

            return outgoing.Invoke(outgoingContext);
        }

        void SetReplyToAddressHeader(Dictionary<string, string> headers)
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
            ((HandlingStageBehavior.Context)context).DoNotInvokeAnyMoreHandlers();
        }

        /// <summary>
        /// <see cref="IBus.CurrentMessageContext"/>
        /// </summary>
        public IMessageContext CurrentMessageContext
        {
            get
            {
                TransportMessage current;

                if (!context.TryGet(TransportReceiveContext.IncomingPhysicalMessageKey, out current))
                {
                    return null;
                }

                return new MessageContext(current);
            }
        }


        void ApplyDefaultDeliveryOptionsIfNeeded(DeliveryOptions options, LogicalMessage logicalMessage)
        {
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

                if (!context.TryGet(TransportReceiveContext.IncomingPhysicalMessageKey, out current))
                {
                    throw new InvalidOperationException("There is no current message being processed");
                }

                return current;
            }
        }
    }
}
