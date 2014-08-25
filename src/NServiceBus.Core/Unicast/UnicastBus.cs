namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using Hosting;
    using Licensing;
    using Logging;
    using MessageInterfaces;
    using Messages;
    using ObjectBuilder;
    using Pipeline;
    using Pipeline.Contexts;
    using Routing;
    using Satellites;
    using Settings;
    using Transport;
    using Transports;

    /// <summary>
    /// A unicast implementation of <see cref="IBus"/> for NServiceBus.
    /// </summary>
    public partial class UnicastBus : IStartableBus, IInMemoryOperations, IManageMessageHeaders
    {
        HostInformation hostInformation = HostInformation.CreateDefault();

        // HACK: Statics are bad, remove
        internal static Guid HostIdForTransportMessageBecauseEverythingIsStaticsInTheConstructor;

        /// <summary>
        /// Initializes a new instance of <see cref="UnicastBus"/>.
        /// </summary>
        public UnicastBus()
        {
            HostIdForTransportMessageBecauseEverythingIsStaticsInTheConstructor = hostInformation.HostId;

            SetupHeaderActions();
        }

        void SetupHeaderActions()
        {
            SetHeaderAction = (message, key, value) =>
            {
                //are we in the process of sending a logical message
                var outgoingLogicalMessageContext = PipelineFactory.CurrentContext as OutgoingContext;

                if (outgoingLogicalMessageContext != null && outgoingLogicalMessageContext.OutgoingLogicalMessage.Instance == message)
                {
                    outgoingLogicalMessageContext.OutgoingLogicalMessage.Headers[key] = value;
                }

                Dictionary<object, Dictionary<string, string>> outgoingHeaders;

                if (!PipelineFactory.CurrentContext.TryGet("NServiceBus.OutgoingHeaders", out outgoingHeaders))
                {
                    outgoingHeaders = new Dictionary<object, Dictionary<string, string>>();

                    PipelineFactory.CurrentContext.Set("NServiceBus.OutgoingHeaders", outgoingHeaders);
                }

                Dictionary<string, string> outgoingHeadersForThisMessage;

                if (!outgoingHeaders.TryGetValue(message, out outgoingHeadersForThisMessage))
                {
                    outgoingHeadersForThisMessage = new Dictionary<string, string>();
                    outgoingHeaders[message] = outgoingHeadersForThisMessage;
                }

                outgoingHeadersForThisMessage[key] = value;
            };

            GetHeaderAction = (message, key) =>
            {
                if (message == ExtensionMethods.CurrentMessageBeingHandled)
                {
                    LogicalMessage messageBeingReceived;

                    //first try to get the header from the current logical message
                    if (PipelineFactory.CurrentContext.TryGet(out messageBeingReceived))
                    {
                        string value;

                        messageBeingReceived.Headers.TryGetValue(key, out value);

                        return value;
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
                    return null;
                }

                Dictionary<object, Dictionary<string, string>> outgoingHeaders;

                if (!PipelineFactory.CurrentContext.TryGet("NServiceBus.OutgoingHeaders", out outgoingHeaders))
                {
                    return null;
                }
                Dictionary<string, string> outgoingHeadersForThisMessage;

                if (!outgoingHeaders.TryGetValue(message, out outgoingHeadersForThisMessage))
                {
                    return null;
                }

                string headerValue;

                outgoingHeadersForThisMessage.TryGetValue(key, out headerValue);

                return headerValue;
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
        /// Provides access to the current host information
        /// </summary>
        public HostInformation HostInformation
        {
            get { return hostInformation; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                HostIdForTransportMessageBecauseEverythingIsStaticsInTheConstructor = value.HostId;
                hostInformation = value;
            }
        }

        /// <summary>
        /// Access to the current settings
        /// </summary>
        public ReadOnlySettings Settings { get; set; }

        /// <summary>
        /// Sets an <see cref="ITransport"/> implementation to use as the
        /// listening endpoint for the bus.
        /// </summary>
        public ITransport Transport { get; set; }

        /// <summary>
        /// Critical error handling
        /// </summary>
        public CriticalError CriticalError { get; set; }

        /// <summary>
        /// Message queue used to send messages.
        /// </summary>
        public ISendMessages MessageSender { get; set; }

        /// <summary>
        /// Configuration.
        /// </summary>
        public Configure Configure { get; set; }

        /// <summary>
        /// Sets <see cref="IBuilder"/> implementation that will be used to 
        /// dynamically instantiate and execute message handlers.
        /// </summary>
        public IBuilder Builder { get; set; }

        /// <summary>
        /// Gets/sets the message mapper.
        /// </summary>
        public IMessageMapper MessageMapper
        {
            get { return messageMapper; }
            set { messageMapper = value; }
        }

        /// <summary>
        /// Sets whether or not the return address of a received message 
        /// should be propagated when the message is forwarded. This field is
        /// used primarily for the Distributor.
        /// </summary>
        public bool PropagateReturnAddressOnSend { get; set; }

        /// <summary>
        /// The router for this <see cref="UnicastBus"/>
        /// </summary>
        public StaticMessageRouter MessageRouter { get; set; }

        /// <summary>
        /// The registered subscription manager for this bus instance
        /// </summary>
        public IManageSubscriptions SubscriptionManager { get; set; }

        /// <summary>
        /// <see cref="IBus.Publish{T}(Action{T})"/>
        /// </summary>
        public void Publish<T>(Action<T> messageConstructor)
        {
            Publish(messageMapper.CreateInstance(messageConstructor));
        }

        /// <summary>
        /// <see cref="IBus.Publish{T}()"/>
        /// </summary>
        public virtual void Publish<T>()
        {
            Publish(messageMapper.CreateInstance<T>());
        }

        /// <summary>
        /// <see cref="IBus.Publish{T}(T)"/>
        /// </summary>
        public virtual void Publish<T>(T message)
        {
            var logicalMessage = LogicalMessageFactory.Create(message);
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

        bool SendOnlyMode { get { return Settings.Get<bool>("Endpoint.SendOnly"); } }

        /// <summary>
        /// <see cref="IBus.Subscribe(Type)"/>
        /// </summary>
        public virtual void Subscribe(Type messageType)
        {
            MessagingBestPractices.AssertIsValidForPubSub(messageType, Builder.Build<Conventions>());

            if (SendOnlyMode)
            {
                throw new InvalidOperationException("It's not allowed for a send only endpoint to be a subscriber");
            }

            AssertHasLocalAddress();

            if (SubscriptionManager == null)
            {
                throw new InvalidOperationException("No subscription manager is available");
            }

            if (TransportDefinition.HasSupportForCentralizedPubSub)
            {
                // We are dealing with a brokered transport wired for auto pub/sub.
                SubscriptionManager.Subscribe(messageType, null);
                return;
            }

            var addresses = GetAtLeastOneAddressForMessageType(messageType);

            foreach (var destination in addresses)
            {
                SubscriptionManager.Subscribe(messageType, destination);
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
            MessagingBestPractices.AssertIsValidForPubSub(messageType, Builder.Build<Conventions>());

            if (SendOnlyMode)
            {
                throw new InvalidOperationException("It's not allowed for a send only endpoint to unsubscribe");
            }

            AssertHasLocalAddress();


            if (SubscriptionManager == null)
            {
                throw new InvalidOperationException("No subscription manager is available");
            }

            if (TransportDefinition.HasSupportForCentralizedPubSub)
            {
                // We are dealing with a brokered transport wired for auto pub/sub.
                SubscriptionManager.Unsubscribe(messageType, null);
                return;
            }

            var addresses = GetAtLeastOneAddressForMessageType(messageType);

            foreach (var destination in addresses)
            {
                SubscriptionManager.Unsubscribe(messageType, destination);
            }

        }

        /// <summary>
        /// <see cref="IBus.Reply(object)"/>
        /// </summary>
        public void Reply(object message)
        {
            var options = new ReplyOptions(MessageBeingProcessed.ReplyToAddress, GetCorrelationId()); 
            
            SendMessage(options, LogicalMessageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="IBus.Reply{T}(Action{T})"/>
        /// </summary>
        public void Reply<T>(Action<T> messageConstructor)
        {
            var instance = messageMapper.CreateInstance(messageConstructor);
            var options = new ReplyOptions(MessageBeingProcessed.ReplyToAddress, GetCorrelationId()); 

            SendMessage(options, LogicalMessageFactory.Create(instance));
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

            var returnMessage = LogicalMessageFactory.CreateControl(new Dictionary<string, string>
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
            if (PipelineFactory.CurrentContext.handleCurrentMessageLaterWasCalled)
            {
                return;
            }

            //if we're a worker, send to the distributor data bus
            if (Settings.GetOrDefault<bool>("Worker.Enabled"))
            {
                MessageSender.Send(MessageBeingProcessed, new SendOptions(Settings.Get<string>("MasterNode.Address")));
            }
            else
            {
                MessageSender.Send(MessageBeingProcessed, new SendOptions(Configure.LocalAddress));
            }

            PipelineFactory.CurrentContext.handleCurrentMessageLaterWasCalled = true;

            ((IncomingContext)PipelineFactory.CurrentContext).DoNotInvokeAnyMoreHandlers();
        }

        /// <summary>
        /// <see cref="IBus.ForwardCurrentMessageTo(string)"/>
        /// </summary>
        public void ForwardCurrentMessageTo(string destination)
        {
            MessageSender.Send(MessageBeingProcessed, new SendOptions(destination));
        }

        /// <summary>
        /// <see cref="IBus.SendLocal{T}(Action{T})"/>
        /// </summary>
        public ICallback SendLocal<T>(Action<T> messageConstructor)
        {
            return SendLocal(messageMapper.CreateInstance(messageConstructor));
        }

        /// <summary>
        /// <see cref="IBus.Send(object)"/>
        /// </summary>
        public ICallback SendLocal(object message)
        {
            //if we're a worker, send to the distributor data bus
            if (Settings.GetOrDefault<bool>("Worker.Enabled"))
            {
                return SendMessage(new SendOptions(Settings.Get<string>("MasterNode.Address")), LogicalMessageFactory.Create(message));
            }
            return SendMessage(new SendOptions(Configure.LocalAddress), LogicalMessageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="IBus.Send{T}(Action{T})"/>
        /// </summary>
        public ICallback Send<T>(Action<T> messageConstructor)
        {
            object message = messageMapper.CreateInstance(messageConstructor);
            var destination = GetDestinationForSend(message);
            return SendMessage(new SendOptions(destination), LogicalMessageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="IBus.Send(object)"/>
        /// </summary>
        public ICallback Send(object message)
        {
            var destination = GetDestinationForSend(message);
            return SendMessage(new SendOptions(destination), LogicalMessageFactory.Create(message));
        }

        string GetDestinationForSend(object message)
        {
            var destinations = GetAtLeastOneAddressForMessageType(message.GetType());

            if (destinations.Count > 1)
            {
                throw new InvalidOperationException("Sends can only target one address.");
            }

            return destinations.SingleOrDefault();
        }

        /// <summary>
        /// <see cref="IBus.Send{T}(string,Action{T})"/>
        /// </summary>
        public ICallback Send<T>(string destination, Action<T> messageConstructor)
        {
            return SendMessage(new SendOptions(destination), LogicalMessageFactory.Create(messageMapper.CreateInstance(messageConstructor)));
        }

        /// <summary>
        /// <see cref="IBus.Send(string,object)"/>
        /// </summary>
        public ICallback Send(string destination, object message)
        {
            return SendMessage(new SendOptions(destination), LogicalMessageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="IBus.Send{T}(string,string,Action{T})"/>
        /// </summary>
        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            var options = new SendOptions(destination)
            {
                CorrelationId = correlationId
            };

            return SendMessage(options, LogicalMessageFactory.Create(messageMapper.CreateInstance(messageConstructor)));
        }

        /// <summary>
        /// <see cref="IBus.Send(string,string,object)"/>
        /// </summary>
        public ICallback Send(string destination, string correlationId, object message)
        {
            var options = new SendOptions(destination)
            {
                CorrelationId = correlationId
            };

            return SendMessage(options, LogicalMessageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="IBus.SendToSites"/>
        /// </summary>
        public ICallback SendToSites(IEnumerable<string> siteKeys, object message)
        {
            this.SetMessageHeader(message, Headers.DestinationSites, string.Join(",", siteKeys.ToArray()));

            var selectedTransportDefinition = Settings.Get<TransportDefinition>();
            var masterNodeAddress = Settings.Get<string>("MasterNode.Address");
            var subScope = selectedTransportDefinition.GetSubScope(masterNodeAddress, "gateway");
            return SendMessage(new SendOptions(subScope), LogicalMessageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="IBus.Defer(System.TimeSpan,object)"/>
        /// </summary>
        public ICallback Defer(TimeSpan delay, object message)
        {
            var options = new SendOptions(Configure.LocalAddress)
            {
                DelayDeliveryWith = delay,
                EnforceMessagingBestPractices = false
            };

            return SendMessage(options, LogicalMessageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="IBus.Defer(DateTime,object)"/>
        /// </summary>
        public ICallback Defer(DateTime processAt, object message)
        {
            var options = new SendOptions(Configure.LocalAddress)
            {
                DeliverAt = processAt,
                EnforceMessagingBestPractices = false
            };
            return SendMessage(options, LogicalMessageFactory.Create(message));
        }


        ICallback SendMessage(SendOptions sendOptions, LogicalMessage message)
        {
            var context = InvokeSendPipeline(sendOptions, message);

            var physicalMessage = context.Get<TransportMessage>();

            return SetupCallback(physicalMessage.Id);
        }

        OutgoingContext InvokeSendPipeline(DeliveryOptions sendOptions, LogicalMessage message)
        {
            if (sendOptions.ReplyToAddress == null && !SendOnlyMode)
            {
                sendOptions.ReplyToAddress = Configure.PublicReturnAddress;
            }

            if (PropagateReturnAddressOnSend && CurrentMessageContext != null)
            {
                sendOptions.ReplyToAddress = CurrentMessageContext.ReplyToAddress;
            }

            return PipelineFactory.InvokeSendPipeline(sendOptions, message);
        }


        ICallback SetupCallback(string transportMessageId)
        {
            var result = new NServiceBus.Callback(transportMessageId);
            result.Registered += delegate(object sender, BusAsyncResultEventArgs args)
            {
                //TODO: what should we do if the key already exists?
                messageIdToAsyncResultLookup[args.MessageId] = args.Result;
            };

            return result;
        }

        /// <summary>
        /// <see cref="IStartableBus.Start()"/>
        /// </summary>
        public IBus Start()
        {
            LicenseManager.PromptUserForLicenseIfTrialHasExpired();

            if (started)
            {
                return this;
            }

            lock (startLocker)
            {
                if (started)
                {
                    return this;
                }

                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

                if (!DoNotStartTransport)
                {
                    Transport.StartedMessageProcessing += TransportStartedMessageProcessing;
                    Transport.TransportMessageReceived += TransportMessageReceived;
                    Transport.FinishedMessageProcessing += TransportFinishedMessageProcessing;
                    Transport.Start(InputAddress);
                }

                started = true;
            }

            satelliteLauncher = new SatelliteLauncher(Builder);
            satelliteLauncher.Start();

            ProcessStartupItems(
                Builder.BuildAll<IWantToRunWhenBusStartsAndStops>().ToList(),
                toRun =>
                {
                    toRun.Start();
                    thingsRanAtStartup.Add(toRun);
                    Log.DebugFormat("Started {0}.", toRun.GetType().AssemblyQualifiedName);
                },
                ex => CriticalError.Raise("Startup task failed to complete.", ex),
                startCompletedEvent);

            return this;
        }

        void ExecuteIWantToRunAtStartupStopMethods()
        {
            Log.Debug("Ensuring IWantToRunWhenBusStartsAndStops.Start has been called.");
            startCompletedEvent.WaitOne();
            Log.Debug("All IWantToRunWhenBusStartsAndStops.Start have completed now.");

            var tasksToStop = Interlocked.Exchange(ref thingsRanAtStartup, new ConcurrentBag<IWantToRunWhenBusStartsAndStops>());
            if (!tasksToStop.Any())
            {
                return;
            }

            ProcessStartupItems(
                tasksToStop,
                toRun =>
                {
                    toRun.Stop();
                    Log.DebugFormat("Stopped {0}.", toRun.GetType().AssemblyQualifiedName);
                },
                ex => Log.Fatal("Startup task failed to stop.", ex),
                stopCompletedEvent);

            stopCompletedEvent.WaitOne();
        }

        /// <summary>
        /// Allow disabling the unicast bus.
        /// </summary>
        public bool DoNotStartTransport { get; set; }

        /// <summary>
        /// The address of this endpoint.
        /// </summary>
        public string InputAddress { get; set; }

        void AssertHasLocalAddress()
        {
            if (Configure.LocalAddress == null)
            {
                throw new InvalidOperationException("Cannot start subscriber without a queue configured. Please specify the LocalAddress property of UnicastBusConfig.");
            }
        }

        /// <summary>
        /// <see cref="IDisposable.Dispose"/>
        /// </summary>
        public void Dispose()
        {
            //Injected at compile time
        }

        void DisposeManaged()
        {
            InnerShutdown();
            Builder.Dispose();
        }

        /// <summary>
        /// <see cref="IBus.DoNotContinueDispatchingCurrentMessageToHandlers"/>
        /// </summary>
        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            ((IncomingContext)PipelineFactory.CurrentContext).DoNotInvokeAnyMoreHandlers();
        }

        /// <summary>
        /// <see cref="IBus.OutgoingHeaders"/>
        /// </summary>
        public IDictionary<string, string> OutgoingHeaders
        {
            get
            {
                return staticOutgoingHeaders;
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

                if (!PipelineFactory.CurrentContext.TryGet(IncomingContext.IncomingPhysicalMessageKey, out current))
                {
                    return null;
                }

                return new MessageContext(current);
            }
        }

        /// <summary>
        /// Shits down the bus and stops processing messages.
        /// </summary>
        public void Shutdown()
        {
            InnerShutdown();
        }

        void InnerShutdown()
        {
            if (!started)
            {
                return;
            }

            Log.Info("Initiating shutdown.");

            if (!DoNotStartTransport)
            {
                Transport.Stop();
                Transport.StartedMessageProcessing -= TransportStartedMessageProcessing;
                Transport.TransportMessageReceived -= TransportMessageReceived;
                Transport.FinishedMessageProcessing -= TransportFinishedMessageProcessing;
            }

            ExecuteIWantToRunAtStartupStopMethods();

            satelliteLauncher.Stop();

            Log.Info("Shutdown complete.");

            started = false;
        }


        void TransportStartedMessageProcessing(object sender, StartedMessageProcessingEventArgs e)
        {
            var incomingMessage = e.Message;

            incomingMessage.Headers[Headers.HostId] = HostInformation.HostId.ToString("N");
            incomingMessage.Headers[Headers.HostDisplayName] = HostInformation.DisplayName;

            PipelineFactory.PreparePhysicalMessagePipelineContext(incomingMessage);

        }
        void TransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            PipelineFactory.InvokeReceivePhysicalMessagePipeline();
        }

        void TransportFinishedMessageProcessing(object sender, FinishedMessageProcessingEventArgs e)
        {
            PipelineFactory.CompletePhysicalMessagePipelineContext();
        }

        /// <summary>
        /// Gets the destination address For a message type.
        /// </summary>
        /// <param name="messageType">The message type to get the destination for.</param>
        /// <returns>The address of the destination associated with the message type.</returns>
        List<string> GetAddressForMessageType(Type messageType)
        {
            var destination = MessageRouter.GetDestinationFor(messageType);

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

        /// <summary>
        /// Map of message identifiers to Async Results - useful for cleanup in case of timeouts.
        /// </summary>
        internal ConcurrentDictionary<string, BusAsyncResult> messageIdToAsyncResultLookup = new ConcurrentDictionary<string, BusAsyncResult>();

        TransportMessage MessageBeingProcessed
        {
            get
            {
                TransportMessage current;

                if (!PipelineFactory.CurrentContext.TryGet(IncomingContext.IncomingPhysicalMessageKey, out current))
                {
                    throw new InvalidOperationException("There is no current message being processed");
                }

                return current;
            }
        }

        volatile bool started;
        object startLocker = new object();

        static ILog Log = LogManager.GetLogger<UnicastBus>();

        ConcurrentBag<IWantToRunWhenBusStartsAndStops> thingsRanAtStartup = new ConcurrentBag<IWantToRunWhenBusStartsAndStops>();
        ManualResetEvent startCompletedEvent = new ManualResetEvent(false);
        ManualResetEvent stopCompletedEvent = new ManualResetEvent(true);

        IMessageMapper messageMapper;
        SatelliteLauncher satelliteLauncher;

        Dictionary<string, string> staticOutgoingHeaders = new Dictionary<string, string>();


        //we need to not inject since at least Autofac doesn't seem to inject internal properties
        PipelineExecutor PipelineFactory
        {
            get
            {
                return Builder.Build<PipelineExecutor>();
            }
        }

        LogicalMessageFactory LogicalMessageFactory
        {
            get
            {
                return Builder.Build<LogicalMessageFactory>();
            }
        }

        TransportDefinition TransportDefinition
        {
            get
            {
                return Builder.Build<TransportDefinition>();
            }
        }

        static void ProcessStartupItems<T>(IEnumerable<T> items, Action<T> iteration, Action<Exception> inCaseOfFault, EventWaitHandle eventToSet)
        {
            eventToSet.Reset();

            Task.Factory.StartNew(() =>
            {
                Parallel.ForEach(items, iteration);
                eventToSet.Set();
            }, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness)
            .ContinueWith(task =>
            {
                eventToSet.Set();
                inCaseOfFault(task.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.LongRunning);
        }
    }
}
