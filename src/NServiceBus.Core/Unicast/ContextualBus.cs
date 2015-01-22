namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

    class ContextualBus : IBus, IManageMessageHeaders, IContextualBus
    {
        readonly IMessageMapper messageMapper;
        readonly Func<BehaviorContext> contextGetter;
        readonly IBuilder builder;
        readonly Configure configure;
        readonly IManageSubscriptions subscriptionManager;
        readonly LogicalMessageFactory messageFactory;
        readonly TransportDefinition transportDefinition;
        readonly ISendMessages messageSender;
        readonly StaticMessageRouter messageRouter;
        readonly CallbackMessageLookup callbackMessageLookup;
        readonly PipelineExecutor pipelineExecutor;
        readonly bool sendOnlyMode;
        readonly Address sendLocalAddress;
        readonly StaticOutgoingMessageHeaders staticOutgoingMessageHeaders;

        public ContextualBus(Func<BehaviorContext> contextGetter, IMessageMapper messageMapper, IBuilder builder, Configure configure, IManageSubscriptions subscriptionManager, 
            MessageMetadataRegistry messageMetadataRegistry, ReadOnlySettings settings, TransportDefinition transportDefinition, ISendMessages messageSender, StaticMessageRouter messageRouter, 
            StaticOutgoingMessageHeaders staticOutgoingMessageHeaders, CallbackMessageLookup callbackMessageLookup, PipelineExecutor pipelineExecutor)
        {
            this.messageMapper = messageMapper;
            this.contextGetter = contextGetter;
            this.builder = builder;
            this.configure = configure;
            this.subscriptionManager = subscriptionManager;
            messageFactory = new LogicalMessageFactory(messageMetadataRegistry, messageMapper, contextGetter);
            this.transportDefinition = transportDefinition;
            this.messageSender = messageSender;
            this.messageRouter = messageRouter;
            this.staticOutgoingMessageHeaders = staticOutgoingMessageHeaders;
            this.callbackMessageLookup = callbackMessageLookup;
            this.pipelineExecutor = pipelineExecutor;
            sendOnlyMode = settings.Get<bool>("Endpoint.SendOnly");
            //if we're a worker, send to the distributor data bus
            if (settings.GetOrDefault<bool>("Worker.Enabled"))
            {
                sendLocalAddress = settings.Get<Address>("MasterNode.Address");
            }
            else
            {
                sendLocalAddress = configure.LocalAddress;
            }
            SetupHeaderActions();
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

        void DisposeManaged()
        {
            //We don't own things, we don't release things
        }

        void SetupHeaderActions()
        {
            SetHeaderAction = (message, key, value) =>
            {
                //are we in the process of sending a logical message
                var outgoingLogicalMessageContext = context as OutgoingContext;

                if (outgoingLogicalMessageContext != null && outgoingLogicalMessageContext.OutgoingLogicalMessage.Instance == message)
                {
                    outgoingLogicalMessageContext.OutgoingLogicalMessage.Headers[key] = value;
                }

                OutgoingHeaders existingHeaders;
                if (!context.TryGet(out existingHeaders))
                {
                    existingHeaders = new OutgoingHeaders();
                    context.Set(existingHeaders);
                }

                existingHeaders.Add(message, key, value);
            };

            GetHeaderAction = (message, key) =>
            {
                if (message == ExtensionMethods.CurrentMessageBeingHandled)
                {
                    //first try to get the header from the current logical message
                    var logicalContext = context as LogicalMessageProcessingStageBehavior.Context;
                    if (logicalContext != null)
                    {
                        string value;

                        if (logicalContext.IncomingLogicalMessage.Headers.TryGetValue(key, out value))
                        {
                            return value;
                        }
                    }

                    //falling back to get the headers from the physical message
                    // when we remove the multi message feature we can remove this and instead
                    // share the same header collection btw physical and logical message
                    if (CurrentMessageContext != null)
                    {
                        string value;
                        if (CurrentMessageContext.Headers.TryGetValue(key, out value))
                        {
                            return value;
                        }
                    }
                }

                OutgoingHeaders existingHeaders;
                if (!context.TryGet(out existingHeaders))
                {
                    return null;
                }

                return existingHeaders.TryGet(message, key);
            };
        }

        /// <summary>
        /// The <see cref="Action{T1,T2,T3}"/> used to set the header in the bus.SetMessageHeader(msg, key, value) method.
        /// </summary>
        public Action<object, string, string> SetHeaderAction { get; internal set; }

        /// <summary>
        /// The <see cref="Func{T1,T2,TResult}"/> used to get the header value in the bus.GetMessageHeader(msg, key) method.
        /// </summary>
        public Func<object, string, string> GetHeaderAction { get; internal set; }

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
        /// <see cref="ISendOnlyBus.Publish{T}(T)"/>
        /// </summary>
        public virtual void Publish<T>(T message)
        {
            var logicalMessage = messageFactory.Create(message);
            var options = new PublishOptions(logicalMessage.MessageType);
            InvokeSendPipeline(options, logicalMessage);
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
                if (Address.Self == destination)
                {
                    throw new InvalidOperationException(string.Format("Message {0} is owned by the same endpoint that you're trying to subscribe", messageType));
                }

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

        List<Address> GetAtLeastOneAddressForMessageType(Type messageType)
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
            var options = new ReplyOptions(MessageBeingProcessed.ReplyToAddress, GetCorrelationId());

            SendMessage(options, messageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="IBus.Reply{T}(Action{T})"/>
        /// </summary>
        public void Reply<T>(Action<T> messageConstructor)
        {
            var instance = messageMapper.CreateInstance(messageConstructor);
            var options = new ReplyOptions(MessageBeingProcessed.ReplyToAddress, GetCorrelationId());

            SendMessage(options, messageFactory.Create(instance));
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

            var returnMessage = messageFactory.CreateControl(new Dictionary<string, string>
            {
                {Headers.ReturnMessageErrorCodeHeader, returnCode}
            });

            var options = new ReplyOptions(MessageBeingProcessed.ReplyToAddress, GetCorrelationId());

            InvokeSendPipeline(options, returnMessage);
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

            messageSender.Send(MessageBeingProcessed, new SendOptions(sendLocalAddress));

            context.handleCurrentMessageLaterWasCalled = true;

            ((HandlingStageBehavior.Context)context).DoNotInvokeAnyMoreHandlers();
        }

        /// <summary>
        /// <see cref="IBus.ForwardCurrentMessageTo(string)"/>
        /// </summary>
        public void ForwardCurrentMessageTo(string destination)
        {
            messageSender.Send(MessageBeingProcessed, new SendOptions(destination));
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
            return SendMessage(new SendOptions(sendLocalAddress), messageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send{T}(Action{T})"/>
        /// </summary>
        public ICallback Send<T>(Action<T> messageConstructor)
        {
            object message = messageMapper.CreateInstance(messageConstructor);
            var destination = GetDestinationForSend(message);
            return SendMessage(new SendOptions(destination), messageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send(object)"/>
        /// </summary>
        public ICallback Send(object message)
        {
            var destination = GetDestinationForSend(message);
            return SendMessage(new SendOptions(destination), messageFactory.Create(message));
        }

        Address GetDestinationForSend(object message)
        {
            var destinations = GetAtLeastOneAddressForMessageType(message.GetType());

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
            return SendMessage(new SendOptions(destination), messageFactory.Create(messageMapper.CreateInstance(messageConstructor)));
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send{T}(Address,Action{T})"/>
        /// </summary>
        public ICallback Send<T>(Address address, Action<T> messageConstructor)
        {
            return SendMessage(new SendOptions(address), messageFactory.Create(messageMapper.CreateInstance(messageConstructor)));
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send(string,object)"/>
        /// </summary>
        public ICallback Send(string destination, object message)
        {
            return SendMessage(new SendOptions(destination), messageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send(Address,object)"/>
        /// </summary>
        public ICallback Send(Address address, object message)
        {
            return SendMessage(new SendOptions(address), messageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send{T}(string,string,Action{T})"/>
        /// </summary>
        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            var options = new SendOptions(destination)
            {
                CorrelationId = correlationId
            };

            return SendMessage(options, messageFactory.Create(messageMapper.CreateInstance(messageConstructor)));
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send{T}(Address,string,Action{T})"/>
        /// </summary>
        public ICallback Send<T>(Address address, string correlationId, Action<T> messageConstructor)
        {
            var options = new SendOptions(address)
            {
                CorrelationId = correlationId
            };

            return SendMessage(options, messageFactory.Create(messageMapper.CreateInstance(messageConstructor)));
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send(string,string,object)"/>
        /// </summary>
        public ICallback Send(string destination, string correlationId, object message)
        {
            var options = new SendOptions(destination)
            {
                CorrelationId = correlationId
            };

            return SendMessage(options, messageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send(Address,string,object)"/>
        /// </summary>
        public ICallback Send(Address address, string correlationId, object message)
        {
            var options = new SendOptions(address)
            {
                CorrelationId = correlationId
            };

            return SendMessage(options, messageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="IBus.Defer(System.TimeSpan,object)"/>
        /// </summary>
        public ICallback Defer(TimeSpan delay, object message)
        {
            var options = new SendOptions(sendLocalAddress)
            {
                DelayDeliveryWith = delay,
                EnforceMessagingBestPractices = false
            };

            return SendMessage(options, messageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="IBus.Defer(DateTime,object)"/>
        /// </summary>
        public ICallback Defer(DateTime processAt, object message)
        {
            var options = new SendOptions(sendLocalAddress)
            {
                DeliverAt = processAt,
                EnforceMessagingBestPractices = false
            };

            return SendMessage(options, messageFactory.Create(message));
        }


        ICallback SendMessage(SendOptions sendOptions, LogicalMessage message)
        {
            var context = InvokeSendPipeline(sendOptions, message);

            var physicalMessage = context.Get<TransportMessage>();

            return SetupCallback(physicalMessage.Id);
        }

        BehaviorContext InvokeSendPipeline(DeliveryOptions sendOptions, LogicalMessage message)
        {
            if (sendOptions.ReplyToAddress == null && !SendOnlyMode)
            {
                sendOptions.ReplyToAddress = configure.PublicReturnAddress;
            }

            if (PropagateReturnAddressOnSend && CurrentMessageContext != null)
            {
                sendOptions.ReplyToAddress = CurrentMessageContext.ReplyToAddress;
            }

            var outgoingContext = new OutgoingContext(context, sendOptions, message);
            return pipelineExecutor.InvokeSendPipeline(outgoingContext);
        }


        ICallback SetupCallback(string transportMessageId)
        {
            var result = new NServiceBus.Callback(transportMessageId, sendOnlyMode);
            result.Registered += (sender, args) => callbackMessageLookup.RegisterResult(args.MessageId, args.Result);

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
        /// <see cref="ISendOnlyBus.OutgoingHeaders"/>
        /// </summary>
        public IDictionary<string, string> OutgoingHeaders
        {
            get
            {
                return staticOutgoingMessageHeaders.OutgoingHeaders;
            }
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

        /// <summary>
        /// 
        /// </summary>
        public IInMemoryOperations InMemory { get; private set; }

        /// <summary>
        /// Gets the destination address For a message type.
        /// </summary>
        /// <param name="messageType">The message type to get the destination for.</param>
        /// <returns>The address of the destination associated with the message type.</returns>
        List<Address> GetAddressForMessageType(Type messageType)
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