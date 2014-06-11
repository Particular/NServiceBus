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
    using Transport;
    using Transports;

    /// <summary>
    /// A unicast implementation of <see cref="IBus"/> for NServiceBus.
    /// </summary>
    public partial class UnicastBus : IStartableBus, IInMemoryOperations
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
        /// Sets an <see cref="ITransport"/> implementation to use as the
        /// listening endpoint for the bus.
        /// </summary>
        public ITransport Transport { get; set; }

        /// <summary>
        /// Message queue used to send messages.
        /// </summary>
        public ISendMessages MessageSender { get; set; }

        /// <summary>
        /// Should be used by programmer, not administrator.
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
            var options = new PublishOptions(typeof(T));

            InvokeSendPipeline(options, LogicalMessageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="IBus.Subscribe{T}()"/>
        /// </summary>
        public void Subscribe<T>()
        {
            Subscribe(typeof(T));
        }

        bool SendOnlyMode { get { return SettingsHolder.Instance.Get<bool>("Endpoint.SendOnly"); } }

        /// <summary>
        /// <see cref="IBus.Subscribe(Type)"/>
        /// </summary>
        public virtual void Subscribe(Type messageType)
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

            var addresses = GetAtLeastOneAddressForMessageType(messageType);

            foreach (var destination in addresses)
            {
                if (Address.Self == destination)
                {
                    throw new InvalidOperationException(string.Format("Message {0} is owned by the same endpoint that you're trying to subscribe", messageType));
                }

                SubscriptionManager.Subscribe(messageType, destination);
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
            var options = new ReplyOptions(MessageBeingProcessed.ReplyToAddress,GetCorrelationId()); 
            
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
            var returnMessage = LogicalMessageFactory.CreateControl(new Dictionary<string, string>
            {
                {Headers.ReturnMessageErrorCodeHeader, errorCode.GetHashCode().ToString()}
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
            if (SettingsHolder.Instance.GetOrDefault<bool>("Worker.Enabled"))
            {
                MessageSender.Send(MessageBeingProcessed, new SendOptions(SettingsHolder.Instance.Get<Address>("MasterNode.Address")));
            }
            else
            {
                MessageSender.Send(MessageBeingProcessed, new SendOptions(Address.Local));
            }

            PipelineFactory.CurrentContext.handleCurrentMessageLaterWasCalled = true;

            IncomingContext.CurrentContext.DoNotInvokeAnyMoreHandlers();
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
            if (SettingsHolder.Instance.GetOrDefault<bool>("Worker.Enabled"))
            {
                return SendMessage(new SendOptions(SettingsHolder.Instance.Get<Address>("MasterNode.Address")), LogicalMessageFactory.Create(message));
            }
            return SendMessage(new SendOptions(Address.Local), LogicalMessageFactory.Create(message));
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
        /// <see cref="IBus.Send{T}(string,Action{T})"/>
        /// </summary>
        public ICallback Send<T>(string destination, Action<T> messageConstructor)
        {
            return SendMessage(new SendOptions(destination), LogicalMessageFactory.Create(messageMapper.CreateInstance(messageConstructor)));
        }

        /// <summary>
        /// <see cref="IBus.Send{T}(Address,Action{T})"/>
        /// </summary>
        public ICallback Send<T>(Address address, Action<T> messageConstructor)
        {
            return SendMessage(new SendOptions(address), LogicalMessageFactory.Create(messageMapper.CreateInstance(messageConstructor)));
        }

        /// <summary>
        /// <see cref="IBus.Send(string,object)"/>
        /// </summary>
        public ICallback Send(string destination, object message)
        {
            return SendMessage(new SendOptions(destination), LogicalMessageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="IBus.Send(Address,object)"/>
        /// </summary>
        public ICallback Send(Address address, object message)
        {
            return SendMessage(new SendOptions(address), LogicalMessageFactory.Create(message));
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
        /// <see cref="IBus.Send{T}(Address,string,Action{T})"/>
        /// </summary>
        public ICallback Send<T>(Address address, string correlationId, Action<T> messageConstructor)
        {
            var options = new SendOptions(address)
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
        /// <see cref="IBus.Send(Address,string,object)"/>
        /// </summary>
        public ICallback Send(Address address, string correlationId, object message)
        {
            var options = new SendOptions(address)
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
            Headers.SetMessageHeader(message, Headers.DestinationSites, string.Join(",", siteKeys.ToArray()));

            return SendMessage(new SendOptions(SettingsHolder.Instance.Get<Address>("MasterNode.Address").SubScope("gateway")), LogicalMessageFactory.Create(message));
        }

        /// <summary>
        /// <see cref="IBus.Defer(System.TimeSpan,object)"/>
        /// </summary>
        public ICallback Defer(TimeSpan delay, object message)
        {
            var options = new SendOptions(Address.Local)
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
            var options = new SendOptions(Address.Local)
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

                Address.PreventChanges();

                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

                if (!DoNotStartTransport)
                {
                    Transport.StartedMessageProcessing += TransportStartedMessageProcessing;
                    Transport.TransportMessageReceived += TransportMessageReceived;
                    Transport.FinishedMessageProcessing += TransportFinishedMessageProcessing;
                    Transport.FailedMessageProcessing += TransportFailedMessageProcessing;
                    Transport.Start(InputAddress);
                }

                started = true;
            }

            satelliteLauncher = new SatelliteLauncher(Builder);
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
                    ConfigureCriticalErrorAction.RaiseCriticalError(String.Format("{0} could not be started.", name), ex);
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
                        ConfigureCriticalErrorAction.RaiseCriticalError(String.Format("{0} could not be stopped.", name), ex);
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
            IncomingContext.CurrentContext.DoNotInvokeAnyMoreHandlers();
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
                Transport.FailedMessageProcessing -= TransportFailedMessageProcessing;
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

        IList<IWantToRunWhenBusStartsAndStops> thingsToRunAtStartup;

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
