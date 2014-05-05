namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;
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
    using Subscriptions.MessageDrivenSubscriptions.SubcriberSideFiltering;
    using Support;
    using Transport;
    using Transports;

    /// <summary>
    /// A unicast implementation of <see cref="IBus"/> for NServiceBus.
    /// </summary>
    public class UnicastBus : IUnicastBus, IInMemoryOperations
    {
        bool messageHandlingDisabled;

        HostInformation hostInformation = HostInformation.CreateDefault();

        // HACK: Statics are bad, remove
        internal static Guid HostIdForTransportMessageBecauseEverythingIsStaticsInTheConstructor;

        public UnicastBus()
        {
            HostIdForTransportMessageBecauseEverythingIsStaticsInTheConstructor = hostInformation.HostId;
        }

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
        /// Should be used by programmer, not administrator.
        /// Disables the handling of incoming messages.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "5.0")]
        public virtual bool DisableMessageHandling
        {
            set
            {
                messageHandlingDisabled = value;
            }
        }


        /// <summary>
        /// Should be used by programmer, not administrator.
        /// Sets an <see cref="ITransport"/> implementation to use as the
        /// listening endpoint for the bus.
        /// </summary>
        public virtual ITransport Transport
        {
            set
            {
                transport = value;
            }
            get { return transport; }
        }

        /// <summary>
        /// Message queue used to send messages.
        /// </summary>
        public ISendMessages MessageSender { get; set; }

        /// <summary>
        /// Information regarding the current master node
        /// </summary>
        public Address MasterNodeAddress { get; set; }

        /// <summary>
        /// Should be used by programmer, not administrator.
        /// Sets <see cref="IBuilder"/> implementation that will be used to 
        /// dynamically instantiate and execute message handlers.
        /// </summary>
        public IBuilder Builder { get; set; }

        /// <summary>
        /// Gets/sets the message mapper.
        /// </summary>
        public virtual IMessageMapper MessageMapper
        {
            get { return messageMapper; }
            set
            {
                messageMapper = value;

                ExtensionMethods.Bus = this;
            }
        }

        /// <summary>
        /// Should be used by programmer, not administrator.
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
        /// Event raised when no subscribers found for the published message.
        /// </summary>
        public event EventHandler<MessageEventArgs> NoSubscribersForMessage;

        /// <summary>
        /// Handles the filtering of messages on the subscriber side
        /// </summary>
        public SubscriptionPredicatesEvaluator SubscriptionPredicatesEvaluator { get; set; }

        /// <summary>
        /// The registered subscription manager for this bus instance
        /// </summary>
        public IManageSubscriptions SubscriptionManager { get; set; }
        
        /// <summary>
        /// Creates an instance of the specified type.
        /// Used primarily for instantiating interface-based messages.
        /// </summary>
        /// <typeparam name="T">The type to instantiate.</typeparam>
        /// <returns>An instance of the specified type.</returns>
        [ObsoleteEx(
            Message = "No longer required since the IBus batch operations have been trimmed",
            TreatAsErrorFromVersion = "4.3",
            RemoveInVersion = "5.0")]
        public T CreateInstance<T>()
        {
            return messageMapper.CreateInstance<T>();
        }

        /// <summary>
        /// Creates an instance of the specified type.
        /// Used primarily for instantiating interface-based messages.
        /// </summary>
        /// <typeparam name="T">The type to instantiate.</typeparam>
        /// <param name="action">An action to perform on the result</param>
        [ObsoleteEx(
            Message = "No longer required since the IBus batch operations have been trimmed",
            TreatAsErrorFromVersion = "4.3",
            RemoveInVersion = "5.0")]
        public T CreateInstance<T>(Action<T> action)
        {
            return messageMapper.CreateInstance(action);
        }

        /// <summary>
        /// Creates an instance of the specified type.
        /// Used primarily for instantiating interface-based messages.
        /// </summary>
        /// <param name="messageType">The type to instantiate.</param>
        /// <returns>An instance of the specified type.</returns>
        [ObsoleteEx(
            Message = "No longer required since the IBus batch operations have been trimmed",
            TreatAsErrorFromVersion = "4.3",
            RemoveInVersion = "5.0")]
        public object CreateInstance(Type messageType)
        {
            return messageMapper.CreateInstance(messageType);
        }

        /// <summary>
        /// Creates an instance of the requested message type (T), 
        /// performing the given action on the created message,
        /// and then publishing it.
        /// </summary>
        public void Publish<T>(Action<T> messageConstructor)
        {
            Publish(CreateInstance(messageConstructor));
        }

        /// <summary>
        /// Publishes the message to all subscribers of the message type.
        /// </summary>
        public virtual void Publish<T>()
        {
            Publish(CreateInstance<T>());
        }

        /// <summary>
        /// Publishes the messages to all subscribers of the first message's type.
        /// </summary>
        public virtual void Publish<T>(T message)
        {
            var sendOptions = new SendOptions
                {
                    Intent = MessageIntentEnum.Publish
                };

            var context = InvokeSendPipeline(sendOptions, LogicalMessageFactory.Create(message));

            if (!context.Get<bool>("SubscribersFound") && NoSubscribersForMessage != null)
            {
                NoSubscribersForMessage(this, new MessageEventArgs(message));
            }
        }

        /// <summary>
        /// Subscribes to the given type - T.
        /// </summary>
        public void Subscribe<T>()
        {
            Subscribe(typeof(T));
        }

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// </summary>
        /// <param name="messageType">The type of message to subscribe to.</param>
        public virtual void Subscribe(Type messageType)
        {
            Subscribe(messageType, null);
        }

        bool SendOnlyMode { get { return SettingsHolder.Get<bool>("Endpoint.SendOnly"); } }

        /// <summary>
        /// Subscribes to receive published messages of the specified type if
        /// they meet the provided condition.
        /// </summary>
        /// <param name="messageType">The type of message to subscribe to.</param>
        /// <param name="condition">The condition under which to receive the message.</param>
        public virtual void Subscribe(Type messageType, Predicate<object> condition)
        {
            MessagingBestPractices.AssertIsValidForPubSub(messageType);

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

            var addresses = GetAddressForMessageType(messageType);
            if (addresses.Count == 0)
            {
                throw new InvalidOperationException(string.Format("No destination could be found for message type {0}. Check the <MessageEndpointMappings> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.", messageType));
            }

            foreach (var destination in addresses)
            {
                if (Address.Self == destination)
                {
                    throw new InvalidOperationException(string.Format("Message {0} is owned by the same endpoint that you're trying to subscribe", messageType));
                }

                SubscriptionManager.Subscribe(messageType, destination);

                if (SubscriptionPredicatesEvaluator != null)
                {
                    SubscriptionPredicatesEvaluator.AddConditionForSubscriptionToMessageType(messageType, condition);
                }
            }
        }

        /// <summary>
        /// Unsubscribes from the given type of message - T.
        /// </summary>
        public void Unsubscribe<T>()
        {
            Unsubscribe(typeof(T));
        }

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        public virtual void Unsubscribe(Type messageType)
        {
            MessagingBestPractices.AssertIsValidForPubSub(messageType);

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

            var addresses = GetAddressForMessageType(messageType);
            if (addresses.Count == 0)
            {
                throw new InvalidOperationException(string.Format("No destination could be found for message type {0}. Check the <MessageEndpointMappings> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.", messageType));
            }

            foreach (var destination in addresses)
            {
                SubscriptionManager.Unsubscribe(messageType, destination);
            }

        }

        public void Reply(object message)
        {
            var options = SendOptions.ReplyTo(MessageBeingProcessed.ReplyToAddress);

            options.CorrelationId = !string.IsNullOrEmpty(MessageBeingProcessed.CorrelationId) ? MessageBeingProcessed.CorrelationId : MessageBeingProcessed.Id;

            SendMessages(options, LogicalMessageFactory.Create(message));
        }

        public void Reply<T>(Action<T> messageConstructor)
        {
            Reply(CreateInstance(messageConstructor));
        }

        public void Return<T>(T errorCode)
        {
            var returnMessage = ControlMessage.Create(Address.Local);

            returnMessage.MessageIntent = MessageIntentEnum.Reply;

            returnMessage.Headers[Headers.ReturnMessageErrorCodeHeader] = errorCode.GetHashCode().ToString();
            returnMessage.CorrelationId = !string.IsNullOrEmpty(MessageBeingProcessed.CorrelationId) ? MessageBeingProcessed.CorrelationId : MessageBeingProcessed.Id;

            var options = SendOptions.ReplyTo(MessageBeingProcessed.ReplyToAddress);

            PipelineFactory.InvokeSendPipeline(options, returnMessage);
        }

        public void HandleCurrentMessageLater()
        {
            if (PipelineFactory.CurrentContext.handleCurrentMessageLaterWasCalled)
            {
                return;
            }

            //if we're a worker, send to the distributor data bus
            if (Configure.Instance.WorkerRunsOnThisEndpoint())
            {
                MessageSender.Send(MessageBeingProcessed, MasterNodeAddress);
            }
            else
            {
                MessageSender.Send(MessageBeingProcessed, Address.Local);
            }

            PipelineFactory.CurrentContext.handleCurrentMessageLaterWasCalled = true;
        }

        public void ForwardCurrentMessageTo(string destination)
        {
            MessageSender.Send(MessageBeingProcessed, Address.Parse(destination));
        }

        public ICallback SendLocal<T>(Action<T> messageConstructor)
        {
            return SendLocal(CreateInstance(messageConstructor));
        }

        public ICallback SendLocal(object message)
        {
            //if we're a worker, send to the distributor data bus
            if (Configure.Instance.WorkerRunsOnThisEndpoint())
            {
                return SendMessages(new SendOptions(MasterNodeAddress), LogicalMessageFactory.Create(message));
            }
            return SendMessages(new SendOptions(Address.Local), LogicalMessageFactory.Create(message));
        }

        public ICallback Send<T>(Action<T> messageConstructor)
        {
            return Send(CreateInstance(messageConstructor));
        }

        public ICallback Send(object message)
        {
            var destinations =  GetAddressForMessageType(message.GetType())
                .Distinct()
                .ToList();

            if (destinations.Count > 1)
            {
                throw new InvalidOperationException("Sends can only target one address.");
            }

            var destination = destinations.SingleOrDefault();

            return SendMessages(new SendOptions(destination), LogicalMessageFactory.Create(message));
        }

        IEnumerable<Address> GetAddressForMessages(IEnumerable<object> messages)
        {
            if (messages == null)
            {
                yield break;
            }

            foreach (var address in messages.SelectMany(message => GetAddressForMessageType(message.GetType())))
            {
                yield return address;
            }
        }

        public ICallback Send<T>(string destination, Action<T> messageConstructor)
        {
            return SendMessages(new SendOptions(destination), LogicalMessageFactory.Create(CreateInstance(messageConstructor)));
        }

        public ICallback Send<T>(Address address, Action<T> messageConstructor)
        {
            return SendMessages(new SendOptions(address), LogicalMessageFactory.Create(CreateInstance(messageConstructor)));
        }

        public ICallback Send(string destination, object message)
        {
            return SendMessages(new SendOptions(destination), LogicalMessageFactory.Create(message));
        }

        public ICallback Send(Address address, object message)
        {
            return SendMessages(new SendOptions(address), LogicalMessageFactory.Create(message));
        }

        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            var options = new SendOptions(destination)
            {
                CorrelationId = correlationId
            };

            return SendMessages(options, LogicalMessageFactory.Create(CreateInstance(messageConstructor)));
        }

        public ICallback Send<T>(Address address, string correlationId, Action<T> messageConstructor)
        {
            var options = new SendOptions(address)
            {
                CorrelationId = correlationId
            };

            return SendMessages(options, LogicalMessageFactory.Create(CreateInstance(messageConstructor)));
        }

        public ICallback Send(string destination, string correlationId, object message)
        {
            var options = new SendOptions(destination)
            {
                CorrelationId = correlationId
            };

            return SendMessages(options, LogicalMessageFactory.Create(message));
        }

        public ICallback Send(Address address, string correlationId, object message)
        {
            var options = new SendOptions(address)
            {
                CorrelationId = correlationId
            };

            return SendMessages(options, LogicalMessageFactory.Create(message));
        }

        public ICallback SendToSites(IEnumerable<string> siteKeys, object message)
        {
            Headers.SetMessageHeader(message, Headers.DestinationSites, string.Join(",", siteKeys.ToArray()));

            return SendMessages(new SendOptions(MasterNodeAddress.SubScope("gateway")), LogicalMessageFactory.Create(message));
        }

        public ICallback Defer(TimeSpan delay, object message)
        {
            var options = new SendOptions(Address.Local)
            {
                DelayDeliveryWith = delay,
                EnforceMessagingBestPractices = false
            };

            return SendMessages(options, LogicalMessageFactory.Create(message));
        }

        public ICallback Defer(DateTime processAt, object message)
        {
            var options = new SendOptions(Address.Local)
            {
                DeliverAt = processAt,
                EnforceMessagingBestPractices = false
            };
            return SendMessages(options, LogicalMessageFactory.Create(message));
        }


        ICallback SendMessages(SendOptions sendOptions, LogicalMessage messages)
        {
            var context = InvokeSendPipeline(sendOptions, messages);

            var physicalMessage = context.Get<TransportMessage>();

            return SetupCallback(physicalMessage.Id);
        }

        SendLogicalMessageContext InvokeSendPipeline(SendOptions sendOptions, LogicalMessage message)
        {
            if (sendOptions.ReplyToAddress == null && !SendOnlyMode)
            {
                sendOptions.ReplyToAddress = Address.Local;
            }

            if (PropagateReturnAddressOnSend && CurrentMessageContext != null)
            {
                sendOptions.ReplyToAddress = CurrentMessageContext.ReplyToAddress;
            }

            return PipelineFactory.InvokeSendPipeline(sendOptions, message);
        }


        ICallback SetupCallback(string transportMessageId)
        {
            var result = new Callback(transportMessageId);
            result.Registered += delegate(object sender, BusAsyncResultEventArgs args)
            {
                //TODO: what should we do if the key already exists?
                messageIdToAsyncResultLookup[args.MessageId] = args.Result;
            };

            return result;
        }

        public event EventHandler Started;

        public IBus Start()
        {
            return Start(() => { });
        }

        public IBus Start(Action startupAction)
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

                Address.PreventChanges();

                if (startupAction != null)
                {
                    startupAction();
                }

                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

                if (!DoNotStartTransport)
                {
                    transport.StartedMessageProcessing += TransportStartedMessageProcessing;
                    transport.TransportMessageReceived += TransportMessageReceived;
                    transport.FinishedMessageProcessing += TransportFinishedMessageProcessing;
                    transport.FailedMessageProcessing += TransportFailedMessageProcessing;
                    transport.Start(InputAddress);
                }

                started = true;
            }

            if (Started != null)
            {
                Started(this, null);
            }

            satelliteLauncher = new SatelliteLauncher { Builder = Builder };
            satelliteLauncher.Start();

            thingsToRunAtStartup = Builder.BuildAll<IWantToRunWhenBusStartsAndStops>().ToList();

            thingsToRunAtStartupTask = thingsToRunAtStartup.Select(toRun => Task.Factory.StartNew(() =>
            {
                var name = toRun.GetType().AssemblyQualifiedName;

                try
                {
                    toRun.Start();
                    Log.DebugFormat("Started {0}.", name);
                }
                catch (Exception ex)
                {
                    Configure.Instance.RaiseCriticalError(String.Format("{0} could not be started.", name), ex);
                }
            }, TaskCreationOptions.LongRunning)).ToArray();

            return this;
        }

        void ExecuteIWantToRunAtStartupStopMethods()
        {
            if (thingsToRunAtStartup == null)
            {
                return;
            }

            //Ensure Start has been called on all thingsToRunAtStartup
            Log.DebugFormat("Ensuring IWantToRunAtStartup.Start has been called.");
            Task.WaitAll(thingsToRunAtStartupTask);
            Log.DebugFormat("All IWantToRunAtStartup.Start should have completed now.");

            var mapTaskToThingsToRunAtStartup = new ConcurrentDictionary<int, string>();

            var tasks = thingsToRunAtStartup.Select(toRun =>
            {
                var name = toRun.GetType().AssemblyQualifiedName;

                var task = new Task(() =>
                {
                    try
                    {
                        toRun.Stop();
                        Log.DebugFormat("Stopped {0}.", name);
                    }
                    catch (Exception ex)
                    {
                        Configure.Instance.RaiseCriticalError(String.Format("{0} could not be stopped.", name), ex);
                    }
                }, TaskCreationOptions.LongRunning);

                mapTaskToThingsToRunAtStartup.TryAdd(task.Id, name);

                task.Start();

                return task;

            }).ToArray();

            Task.WaitAll(tasks);
        }

        /// <summary>
        /// Allow disabling the unicast bus.
        /// </summary>
        public bool DoNotStartTransport { get; set; }

        /// <summary>
        /// The address this bus will use as it's main input
        /// </summary>
        public Address InputAddress
        {
            get
            {
                if (inputAddress == null)
                    inputAddress = Address.Local;

                return inputAddress;
            }
            set { inputAddress = value; }
        }

        void AssertHasLocalAddress()
        {
            if (Address.Local == null)
                throw new InvalidOperationException("Cannot start subscriber without a queue configured. Please specify the LocalAddress property of UnicastBusConfig.");
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public void DisposeManaged()
        {
            InnerShutdown();
            Configure.Instance.Builder.Dispose();
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            PipelineFactory.CurrentContext.AbortChain();
        }

        public IDictionary<string, string> OutgoingHeaders
        {
            get
            {
                return staticOutgoingHeaders;
            }
        }

        public IMessageContext CurrentMessageContext
        {
            get
            {
                TransportMessage current;

                if (!PipelineFactory.CurrentContext.TryGet(ReceivePhysicalMessageContext.IncomingPhysicalMessageKey, out current))
                {
                    return null;
                }

                return new MessageContext(current);
            }
        }

        public IInMemoryOperations InMemory
        {
            get
            {
                ThrowInMemoryException();
                return null;
            }
        }

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
                transport.Stop();
                transport.StartedMessageProcessing -= TransportStartedMessageProcessing;
                transport.TransportMessageReceived -= TransportMessageReceived;
                transport.FinishedMessageProcessing -= TransportFinishedMessageProcessing;
                transport.FailedMessageProcessing -= TransportFailedMessageProcessing;
            }

            ExecuteIWantToRunAtStartupStopMethods();

            satelliteLauncher.Stop();

            Log.Info("Shutdown complete.");

            started = false;
        }

        public void Raise<T>(Action<T> messageConstructor)
        {
            ThrowInMemoryException();
        }

        public void Raise<T>(T @event)
        {
            ThrowInMemoryException();
        }

        static void ThrowInMemoryException()
        {
            throw new Exception("InMemory.Raise has been removed from the core please see http://docs.particular.net/nservicebus/inmemoryremoval");
        }

        void TransportStartedMessageProcessing(object sender, StartedMessageProcessingEventArgs e)
        {
            var incomingMessage = e.Message;

            incomingMessage.Headers["NServiceBus.ProcessingMachine"] = RuntimeEnvironment.MachineName;
            incomingMessage.Headers[Headers.ProcessingEndpoint] = Configure.EndpointName;
            incomingMessage.Headers[Headers.HostId] = HostInformation.HostId.ToString("N");
            incomingMessage.Headers[Headers.HostDisplayName] = HostInformation.DisplayName;

            PipelineFactory.PreparePhysicalMessagePipelineContext(incomingMessage, messageHandlingDisabled);

#pragma warning disable 0618
            modules = Builder.BuildAll<IMessageModule>().ToList();
#pragma warning restore 0618

            modules.ForEach(module =>
            {
                Log.Debug("Calling 'HandleBeginMessage' on " + module.GetType().FullName);
                module.HandleBeginMessage(); //don't need to call others if one fails                                    
            });

            modules.Reverse();//make sure that the modules are called in reverse order when processing ends
        }
        void TransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            PipelineFactory.InvokeReceivePhysicalMessagePipeline();
        }

        void TransportFinishedMessageProcessing(object sender, FinishedMessageProcessingEventArgs e)
        {
            try
            {
                modules.ForEach(module =>
                {
                    Log.Debug("Calling 'HandleEndMessage' on " + module.GetType().FullName);
                    module.HandleEndMessage();
                });
            }
            finally
            {
                PipelineFactory.CompletePhysicalMessagePipelineContext();
            }
        }

        void TransportFailedMessageProcessing(object sender, FailedMessageProcessingEventArgs e)
        {
            if (modules == null)
            {
                return;
            }

            modules.ForEach(module =>
            {
                Log.Debug("Calling 'HandleError' on " + module.GetType().FullName);
                module.HandleError();
            });
        }

        /// <summary>
        /// Gets the destination address For a message type.
        /// </summary>
        /// <param name="messageType">The message type to get the destination for.</param>
        /// <returns>The address of the destination associated with the message type.</returns>
        List<Address> GetAddressForMessageType(Type messageType)
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

        Address inputAddress;


#pragma warning disable 0618
        /// <summary>
        /// Thread-static list of message modules, needs to be initialized for every transport message
        /// </summary>
        [ThreadStatic]
        static List<IMessageModule> modules;
#pragma warning restore 0618

        /// <summary>
        /// Map of message identifiers to Async Results - useful for cleanup in case of timeouts.
        /// </summary>
        internal ConcurrentDictionary<string, BusAsyncResult> messageIdToAsyncResultLookup = new ConcurrentDictionary<string, BusAsyncResult>();

        TransportMessage MessageBeingProcessed
        {
            get
            {
                TransportMessage current;

                if (!PipelineFactory.CurrentContext.TryGet(ReceivePhysicalMessageContext.IncomingPhysicalMessageKey, out current))
                {
                    throw new InvalidOperationException("There is no current message being processed");
                }

                return current;
            }
        }

        volatile bool started;
        object startLocker = new object();

        static ILog Log = LogManager.GetLogger(typeof(UnicastBus));

        IList<IWantToRunWhenBusStartsAndStops> thingsToRunAtStartup;

#pragma warning disable 3005
        protected ITransport transport;
#pragma warning restore 3005

        IMessageMapper messageMapper;
        Task[] thingsToRunAtStartupTask = new Task[0];
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


    }
}
