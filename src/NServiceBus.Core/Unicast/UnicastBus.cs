namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Security.Principal;
    using System.Text;
    using System.Threading.Tasks;
    using Audit;
    using Licensing;
    using Logging;
    using MessageInterfaces;
    using MessageMutator;
    using Messages;
    using ObjectBuilder;
    using Pipeline;
    using Pipeline.Behaviors;
    using Queuing;
    using Routing;
    using Sagas;
    using Satellites;
    using Serialization;
    using Subscriptions;
    using Subscriptions.MessageDrivenSubscriptions.SubcriberSideFiltering;
    using Support;
    using Transport;
    using Transports;
    using UnitOfWork;

    /// <summary>
    /// A unicast implementation of <see cref="IBus"/> for NServiceBus.
    /// </summary>
    public class UnicastBus : IUnicastBus, IInMemoryOperations
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public UnicastBus()
        {
            _messageBeingHandled = null;
        }

        /// <summary>
        /// Should be used by programmer, not administrator.
        /// Disables the handling of incoming messages.
        /// </summary>
        public virtual bool DisableMessageHandling
        {
            set { disableMessageHandling = value; }
        }
        bool disableMessageHandling;


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

                transport.StartedMessageProcessing += TransportStartedMessageProcessing;
                transport.TransportMessageReceived += TransportMessageReceived;
                transport.FinishedMessageProcessing += TransportFinishedMessageProcessing;
                transport.FailedMessageProcessing += TransportFailedMessageProcessing;
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

        [ObsoleteEx(RemoveInVersion = "5.0")]
        public delegate void MessageReceivedDelegate(TransportMessage message);

        /// <summary>
        /// Event raised when a message is received.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "5.0")]
        public event MessageReceivedDelegate MessageReceived;

        internal void OnMessageReceived(TransportMessage message)
        {
            var onMessageReceived = MessageReceived;
            if (onMessageReceived != null)
            {
                onMessageReceived(message);
            }
        }

        /// <summary>
        /// Event raised when messages are sent.
        /// </summary>
        public event EventHandler<MessagesEventArgs> MessagesSent;

        /// <summary>
        /// Clear Timeouts For the saga
        /// </summary>
        /// <param name="sagaId">Id of the Saga for clearing the timeouts</param>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "IDeferMessages.ClearDeferredMessages")]
        public void ClearTimeoutsFor(Guid sagaId)
        {
            if (sagaId == Guid.Empty)
            {
                throw new ArgumentException("Invalid saga id.", "sagaId");
            }

            MessageDeferrer.ClearDeferredMessages(Headers.SagaId, sagaId.ToString());
        }


        /// <summary>
        /// Should be used by the programmer, not the administrator.
        /// Gets and sets an <see cref="IMessageSerializer"/> implementation to
        /// be used for subscription storage for the bus.
        /// </summary>
        public virtual IMessageSerializer MessageSerializer { get; set; }


        /// <summary>
        /// The registry of all known messages for this endpoint
        /// </summary>
        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }


        /// <summary>
        /// A way to request the transport to defer the processing of a message
        /// </summary>
        public IDeferMessages MessageDeferrer { get; set; }

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

                ExtensionMethods.MessageCreator = value;
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


        [ObsoleteEx(RemoveInVersion = "5.0")]
        public Address ForwardReceivedMessagesTo { get; set; }


        [ObsoleteEx(RemoveInVersion = "5.0")]
        public TimeSpan TimeToBeReceivedOnForwardedMessages { get; set; }


        [ObsoleteEx(RemoveInVersion = "5.0")]
        //TODO: how do we handle this?
        public MessageAuditer MessageAuditer { get; set; }

        /// <summary>
        /// The router for this <see cref="UnicastBus"/>
        /// </summary>
        public IRouteMessages MessageRouter { get; set; }

        /// <summary>
        /// Event raised when no subscribers found for the published message.
        /// </summary>
        public event EventHandler<MessageEventArgs> NoSubscribersForMessage;

        /// <summary>
        /// Event raised when client subscribed to a message type.
        /// </summary>
        event EventHandler<SubscriptionEventArgs> IUnicastBus.ClientSubscribed
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }


        /// <summary>
        /// Handles the filtering of messages on the subscriber side
        /// </summary>
        public SubscriptionPredicatesEvaluator SubscriptionPredicatesEvaluator { get; set; }

        /// <summary>
        /// The registered subscription manager for this bus instance
        /// </summary>
        public IManageSubscriptions SubscriptionManager { get; set; }

        /// <summary>
        /// Publishes the given messages
        /// </summary>
        public IPublishMessages MessagePublisher { get; set; }

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
        public virtual void Publish<T>(T message)
        {
            Publish(new[] { message });
        }

        /// <summary>
        /// Publishes the message to all subscribers of the message type.
        /// </summary>
        public virtual void Publish<T>()
        {
            Publish(new object[] { });
        }

        /// <summary>
        /// Publishes the messages to all subscribers of the first message's type.
        /// </summary>
        public virtual void Publish<T>(params T[] messages)
        {

            if (messages == null || messages.Length == 0) // Bus.Publish<IFoo>();
            {
                Publish(CreateInstance<T>(m => { }));
                return;
            }

            MessagingBestPractices.AssertIsValidForPubSub(messages[0].GetType());

            var fullTypes = GetFullTypes(messages as object[]);
            var eventMessage = new TransportMessage { MessageIntent = MessageIntentEnum.Publish };

            MapTransportMessageFor(messages as object[], eventMessage);

            if (MessagePublisher == null)
                throw new InvalidOperationException("No message publisher has been registered. If you're using a transport without native support for pub/sub please enable the message driven publishing feature by calling: Feature.Enable<MessageDrivenPublisher>() in your configuration");

            var subscribersExisted = MessagePublisher.Publish(eventMessage, fullTypes);

            if (!subscribersExisted && NoSubscribersForMessage != null)
            {
                NoSubscribersForMessage(this, new MessageEventArgs(messages[0]));
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

        /// <summary>
        /// Subscribes to the given type T, registering a condition that all received
        /// messages of that type should comply with, otherwise discarding them.
        /// </summary>
        public void Subscribe<T>(Predicate<T> condition)
        {
            var p = new Predicate<object>(m =>
            {
                if (m is T)
                    return condition((T)m);

                return true;
            }
            );

            Subscribe(typeof(T), p);
        }

        /// <summary>
        /// Subscribes to receive published messages of the specified type if
        /// they meet the provided condition.
        /// </summary>
        /// <param name="messageType">The type of message to subscribe to.</param>
        /// <param name="condition">The condition under which to receive the message.</param>
        public virtual void Subscribe(Type messageType, Predicate<object> condition)
        {
            MessagingBestPractices.AssertIsValidForPubSub(messageType);

            if (Configure.SendOnlyMode)
                throw new InvalidOperationException("It's not allowed for a send only endpoint to be a subscriber");

            AssertHasLocalAddress();

            var destination = GetAddressForMessageType(messageType);
            if (Address.Self == destination)
                throw new InvalidOperationException(string.Format("Message {0} is owned by the same endpoint that you're trying to subscribe", messageType));


            if (SubscriptionManager == null)
                throw new InvalidOperationException("No subscription manager is available");

            SubscriptionManager.Subscribe(messageType, destination);

            if (SubscriptionPredicatesEvaluator != null)
                SubscriptionPredicatesEvaluator.AddConditionForSubscriptionToMessageType(messageType, condition);
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

            if (Configure.SendOnlyMode)
                throw new InvalidOperationException("It's not allowed for a send only endpoint to unsubscribe");

            AssertHasLocalAddress();

            var destination = GetAddressForMessageType(messageType);

            if (SubscriptionManager == null)
                throw new InvalidOperationException("No subscription manager is available");

            SubscriptionManager.Unsubscribe(messageType, destination);
        }


        public void Reply(params object[] messages)
        {
            MessagingBestPractices.AssertIsValidForReply(messages.ToList());
            if (_messageBeingHandled.ReplyToAddress == null)
                throw new InvalidOperationException("Reply was called with null reply-to-address field. It can happen if you are using a SendOnly client. See http://particular.net/articles/one-way-send-only-endpoints");
            SendMessage(_messageBeingHandled.ReplyToAddress, _messageBeingHandled.CorrelationId ?? _messageBeingHandled.Id, MessageIntentEnum.Reply, messages);
        }

        public void Reply(object message)
        {
            Reply(new[] { message });
        }

        public void Reply<T>(Action<T> messageConstructor)
        {
            Reply(CreateInstance(messageConstructor));
        }

        public void Return<T>(T errorCode)
        {
            if (_messageBeingHandled.ReplyToAddress == null)
                throw new InvalidOperationException("Return was called with null reply-to-address field. It can happen if you are using a SendOnly client. See http://particular.net/articles/one-way-send-only-endpoints");

            var returnMessage = ControlMessage.Create(Address.Local);

            returnMessage.MessageIntent = MessageIntentEnum.Reply;

            returnMessage.Headers[Headers.ReturnMessageErrorCodeHeader] = errorCode.GetHashCode().ToString();
            returnMessage.CorrelationId = _messageBeingHandled.CorrelationId ?? _messageBeingHandled.Id;

            InvokeOutgoingTransportMessagesMutators(new object[] { }, returnMessage);
            MessageSender.Send(returnMessage, _messageBeingHandled.ReplyToAddress);
        }

        public void HandleCurrentMessageLater()
        {
            if (contextStacker.Current.handleCurrentMessageLaterWasCalled)
            {
                return;
            }

            //if we're a worker, send to the distributor data bus
            if (Configure.Instance.WorkerRunsOnThisEndpoint())
            {
                MessageSender.Send(_messageBeingHandled, MasterNodeAddress);
            }
            else
            {
                MessageSender.Send(_messageBeingHandled, Address.Local);
            }

            contextStacker.Current.handleCurrentMessageLaterWasCalled = true;
        }

        public void ForwardCurrentMessageTo(string destination)
        {
            MessageSender.Send(_messageBeingHandled, Address.Parse(destination));
        }

        public ICallback SendLocal<T>(Action<T> messageConstructor)
        {
            return SendLocal(CreateInstance(messageConstructor));
        }

        public ICallback SendLocal(object message)
        {
            return SendLocal(new[] { message });
        }

        public ICallback SendLocal(params object[] messages)
        {
            //if we're a worker, send to the distributor data bus
            if (Configure.Instance.WorkerRunsOnThisEndpoint())
            {
                return Send(MasterNodeAddress, messages);
            }

            return Send(Address.Local, messages);
        }

        public ICallback Send<T>(Action<T> messageConstructor)
        {
            return Send(CreateInstance(messageConstructor));
        }

        public ICallback Send(object message)
        {
            return Send(new[] { message });
        }

        public ICallback Send(params object[] messages)
        {
            var destination = GetAddressForMessages(messages);

            return SendMessage(destination, null, MessageIntentEnum.Send, messages);
        }

        public ICallback Send<T>(string destination, Action<T> messageConstructor)
        {
            return SendMessage(destination, null, MessageIntentEnum.Send, CreateInstance(messageConstructor));
        }

        public ICallback Send<T>(Address address, Action<T> messageConstructor)
        {
            return SendMessage(address, null, MessageIntentEnum.Send, CreateInstance(messageConstructor));
        }

        public ICallback Send(string destination, object message)
        {
            return SendMessage(destination, null, MessageIntentEnum.Send, new[] { message });
        }

        public ICallback Send(string destination, params object[] messages)
        {
            return SendMessage(destination, null, MessageIntentEnum.Send, messages);
        }

        public ICallback Send(Address address, params object[] messages)
        {
            return SendMessage(address, null, MessageIntentEnum.Send, messages);
        }

        public ICallback Send(Address address, object message)
        {
            return SendMessage(address, null, MessageIntentEnum.Send, new[] { message });
        }

        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            return SendMessage(destination, correlationId, MessageIntentEnum.Send, CreateInstance(messageConstructor));
        }

        public ICallback Send<T>(Address address, string correlationId, Action<T> messageConstructor)
        {
            return SendMessage(address, correlationId, MessageIntentEnum.Send, CreateInstance(messageConstructor));
        }

        public ICallback Send(string destination, string correlationId, object message)
        {
            return Send(destination, correlationId, new[] { message });
        }

        public ICallback Send(string destination, string correlationId, params object[] messages)
        {
            return SendMessage(destination, correlationId, MessageIntentEnum.Send, messages);
        }

        public ICallback Send(Address address, string correlationId, params object[] messages)
        {
            return SendMessage(address, correlationId, MessageIntentEnum.Send, messages);
        }

        public ICallback Send(Address address, string correlationId, object message)
        {
            return SendMessage(address, correlationId, MessageIntentEnum.Send, new[] { message });
        }

        public ICallback SendToSites(IEnumerable<string> siteKeys, object message)
        {
            return SendToSites(siteKeys, new[] { message });
        }

        public ICallback SendToSites(IEnumerable<string> siteKeys, params object[] messages)
        {
            if (messages == null || messages.Length == 0)
                throw new InvalidOperationException("Cannot send an empty set of messages.");

            Headers.SetMessageHeader(messages[0], Headers.DestinationSites, string.Join(",", siteKeys.ToArray()));

            return SendMessage(MasterNodeAddress.SubScope("gateway"), null, MessageIntentEnum.Send, messages);
        }

        public ICallback Defer(TimeSpan delay, params object[] messages)
        {
            return Defer(DateTime.UtcNow + delay, messages);
        }

        public ICallback Defer(TimeSpan delay, object message)
        {
            return Defer(DateTime.UtcNow + delay, message);
        }

        public ICallback Defer(DateTime processAt, object message)
        {
            return Defer(processAt, new[] { message });
        }

        public ICallback Defer(DateTime processAt, params object[] messages)
        {
            if (messages == null || messages.Length == 0)
            {
                throw new InvalidOperationException("Cannot Defer an empty set of messages.");
            }
            if (processAt.ToUniversalTime() <= DateTime.UtcNow)
            {
                return SendLocal(messages);
            }

            var toSend = new TransportMessage();

            MapTransportMessageFor(messages, toSend);

            toSend.Headers[Headers.IsDeferredMessage] = Boolean.TrueString;

            MessageDeferrer.Defer(toSend, processAt, Address.Local);

            return SetupCallback(toSend.Id);
        }

        private ICallback SendMessage(string destination, string correlationId, MessageIntentEnum messageIntent, params object[] messages)
        {
            if (messages == null || messages.Length == 0)
                throw new InvalidOperationException("Cannot send an empty set of messages.");

            if (destination == null)
                throw new InvalidOperationException(
                    string.Format("No destination specified for message {0}. Message cannot be sent. Check the UnicastBusConfig section in your config file and ensure that a MessageEndpointMapping exists for the message type.", messages[0].GetType().FullName));

            return SendMessage(Address.Parse(destination), correlationId, messageIntent, messages);
        }

        private ICallback SendMessage(Address address, string correlationId, MessageIntentEnum messageIntent, params object[] messages)
        {
            // loop only happens once
            foreach (var id in SendMessage(new List<Address> { address }, correlationId, messageIntent, messages))
            {
                return SetupCallback(id);
            }

            return null;
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

        IEnumerable<string> SendMessage(List<Address> addresses, string correlationId, MessageIntentEnum messageIntent, params object[] messages)
        {
            if (messages.Length == 0)
            {
                return Enumerable.Empty<string>();
            }

            messages.ToList()
                        .ForEach(message => MessagingBestPractices.AssertIsValidForSend(message.GetType(), messageIntent));

            if (messages.Length > 1)
            {
                // Users can't send more than one message with a DataBusProperty in the same TransportMessage, Yes this is a limitation for now!
                var numberOfMessagesWithDataBusProperties = 0;
                foreach (var message in messages)
                {
                    var hasAtLeastOneDataBusProperty = message.GetType().GetProperties().Any(MessageConventionExtensions.IsDataBusProperty);

                    if (hasAtLeastOneDataBusProperty)
                    {
                        numberOfMessagesWithDataBusProperties++;
                    }
                }

                if (numberOfMessagesWithDataBusProperties > 1)
                {
                    throw new InvalidOperationException("This version of NServiceBus only supports sending up to one message with DataBusProperties per Send().");
                }
            }

            addresses
                .ForEach(address =>
                             {
                                 if (address == Address.Undefined)
                                     throw new InvalidOperationException("No destination specified for message(s): " +
                                                                         string.Join(";", messages.Select(m => m.GetType())));
                             });



            var result = new List<string>();

            var toSend = new TransportMessage { MessageIntent = messageIntent };

            if (!string.IsNullOrEmpty(correlationId))
            {
                toSend.CorrelationId = correlationId;
            }

            MapTransportMessageFor(messages, toSend);

            foreach (var destination in addresses)
            {
                try
                {
                    MessageSender.Send(toSend, destination);
                }
                catch (QueueNotFoundException ex)
                {
                    throw new ConfigurationErrorsException("The destination queue '" + destination +
                                                         "' could not be found. You may have misconfigured the destination for this kind of message (" +
                                                        messages[0].GetType().FullName +
                                                         ") in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file. " +
                                                         "It may also be the case that the given queue just hasn't been created yet, or has been deleted."
                                                        , ex);
                }

                if (Log.IsDebugEnabled)
                    Log.Debug(string.Format("Sending message {0} with ID {1} to destination {2}.\n" +
                                            "ToString() of the message yields: {3}\n" +
                                            "Message headers:\n{4}",
                                            messages[0].GetType().AssemblyQualifiedName,
                                            toSend.Id,
                                            destination,
                                            messages[0],
                                            string.Join(", ", toSend.Headers.Select(h => h.Key + ":" + h.Value).ToArray())
                        ));

                result.Add(toSend.Id);
            }

            if (MessagesSent != null)
                MessagesSent(this, new MessagesEventArgs(messages));

            return result;
        }

        List<Type> GetFullTypes(IEnumerable<object> messages)
        {
            var types = new List<Type>();

            foreach (var m in messages)
            {
                var messageType = m.GetType();
                var s = MessageMapper.GetMappedTypeFor(messageType);
                if (types.Contains(s))
                {
                    continue;
                }

                types.Add(s);

                foreach (var t in GetParentTypes(messageType)
                    .Where(MessageConventionExtensions.IsMessageType)
                    .Where(t => !types.Contains(t)))
                {
                    types.Add(t);
                }
            }

            return types;
        }

        static IEnumerable<Type> GetParentTypes(Type type)
        {
            foreach (var i in type.GetInterfaces())
            {
                yield return i;
            }

            // return all inherited types
            var currentBaseType = type.BaseType;
            var objectType = typeof(Object);
            while (currentBaseType != null && currentBaseType != objectType)
            {
                yield return currentBaseType;
                currentBaseType = currentBaseType.BaseType;
            }
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
                return this;

            lock (startLocker)
            {
                if (started)
                    return this;

                starting = true;

                Address.PreventChanges();

                ValidateConfiguration();

                if (startupAction != null)
                {
                    startupAction();
                }

                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);


                if (!DoNotStartTransport)
                {
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
                    Log.DebugFormat("Starting {0}.", name);
                    toRun.Start();
                    Log.DebugFormat("Started {0}.", name);
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("{0} could not be started.", ex, name);
                    //don't rethrow so that thread doesn't die before log message is shown.
                }
            }, TaskCreationOptions.LongRunning)).ToArray();

            return this;
        }

        private void ExecuteIWantToRunAtStartupStopMethods()
        {
            if (thingsToRunAtStartup == null)
                return;

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
                                Log.DebugFormat("Stopping {0}.", name);
                                toRun.Stop();
                                Log.DebugFormat("Stopped {0}.", name);
                            }
                            catch (Exception ex)
                            {
                                Log.ErrorFormat("{0} could not be stopped.", ex, name);
                                // no need to rethrow, closing the process anyway
                            }
                        }, TaskCreationOptions.LongRunning);

                    mapTaskToThingsToRunAtStartup.TryAdd(task.Id, name);

                    task.Start();

                    return task;

                }).ToArray();

            // Wait for a period here otherwise the process may be killed too early!
            var timeout = TimeSpan.FromSeconds(20);
            if (Task.WaitAll(tasks, timeout))
            {
                return;
            }

            Log.WarnFormat("Not all IWantToRunWhenBusStartsAndStops.Stop methods were successfully called within {0}secs", timeout.Seconds);

            var sb = new StringBuilder();
            foreach (var task in tasks.Where(task => !task.IsCompleted))
            {
                sb.AppendLine(mapTaskToThingsToRunAtStartup[task.Id]);
            }

            Log.WarnFormat("List of tasks that did not finish within {0}secs:\n{1}", timeout.Seconds, sb.ToString());
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

        void ValidateConfiguration()
        {
            if (!SkipDeserialization && MessageSerializer == null)
                throw new InvalidOperationException("No message serializer has been configured.");
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public void DisposeManaged()
        {
            InnerShutdown();
            contextStacker.Dispose();
            Configure.Instance.Builder.Dispose();
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            var context = contextStacker.Current;

            if (context == null)
            {
                throw new InvalidOperationException("DoNotContinueDispatchingCurrentMessageToHandlers() is only valid to call when receiving a message");
            }

            context.AbortChain();
        }

        public IDictionary<string, string> OutgoingHeaders
        {
            get
            {
                return ExtensionMethods.GetStaticOutgoingHeadersAction();
            }
        }

        public IMessageContext CurrentMessageContext
        {
            get
            {
                return _messageBeingHandled == null ? null : new MessageContext(_messageBeingHandled);
            }
        }

        public IInMemoryOperations InMemory
        {
            get { return this; }
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

            ExecuteIWantToRunAtStartupStopMethods();

            satelliteLauncher.Stop();

            transport.Stop();
            transport.StartedMessageProcessing -= TransportStartedMessageProcessing;
            transport.TransportMessageReceived -= TransportMessageReceived;
            transport.FinishedMessageProcessing -= TransportFinishedMessageProcessing;
            transport.FailedMessageProcessing -= TransportFailedMessageProcessing;

            Log.Info("Shutdown complete.");

            started = false;
        }


        /// <summary>
        /// The list of message dispatcher factories to use
        /// </summary>
        public IDictionary<Type, Type> MessageDispatcherMappings { get; set; }

        [ObsoleteEx(RemoveInVersion = "5.0")]
        public bool SkipDeserialization
        {
            get { return skipDeserialization; }
            set { skipDeserialization = value; }
        }
        internal bool skipDeserialization;


        /// <summary>
        /// Handles the <see cref="ITransport.TransportMessageReceived"/> event from the <see cref="ITransport"/> used
        /// for the bus.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The arguments for the event.</param>
        /// <remarks>
        /// When the transport passes up the <see cref="TransportMessage"/> its received,
        /// the bus checks for initialization, 
        /// sets the message as that which is currently being handled for the current thread
        /// and, depending on <see cref="DisableMessageHandling"/>, attempts to handle the message.
        /// </remarks>
        private void TransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            using (var child = Builder.CreateChildBuilder())
            {
                HandleTransportMessage(child, e.Message);
            }
        }


        public void Raise<T>(T @event)
        {
            var chain = new BehaviorChain(Builder, contextStacker);

            chain.Add<LoadHandlersBehavior>();
            chain.Add<InvokeHandlersBehavior>();

            using (var context = new BehaviorContext(Builder, new TransportMessage(), contextStacker))
            {
                var logicalMessages = new LogicalMessages {new LogicalMessage(typeof(T),@event)};

                context.Set(logicalMessages);
                chain.Invoke(context);
            }
        }

        public void Raise<T>(Action<T> messageConstructor)
        {
            Raise(CreateInstance(messageConstructor));
        }


        void HandleTransportMessage(IBuilder childBuilder, TransportMessage msg)
        {
            // construct behavior chain - look at configuration and possibly the incoming transport message
            var chain = new BehaviorChain(childBuilder, contextStacker);
            chain.Add<MessageHandlingLoggingBehavior>();

            if (ConfigureImpersonation.Impersonate)
            {
                chain.Add<ImpersonateSenderBehavior>();
            }

            chain.Add<AuditBehavior>();
            chain.Add<ForwardBehavior>();
            chain.Add<UnitOfWorkBehavior>();
            chain.Add<ApplyIncomingTransportMessageMutatorsBehavior>();
            chain.Add<RaiseMessageReceivedBehavior>();

            if (!disableMessageHandling)
            {
                chain.Add<ExtractLogicalMessagesBehavior>();
                chain.Add<ApplyIncomingMessageMutatorsBehavior>();
                chain.Add<CallbackInvocationBehavior>();
                chain.Add<LoadHandlersBehavior>();
                chain.Add<SagaPersistenceBehavior>();
                chain.Add<InvokeHandlersBehavior>();
            }

            chain.Invoke(msg);
        }


        void TransportFinishedMessageProcessing(object sender, FinishedMessageProcessingEventArgs e)
        {
            modules.ForEach(module =>
            {
                Log.Debug("Calling 'HandleEndMessage' on " + module.GetType().FullName);
                module.HandleEndMessage();
            });
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

        void TransportStartedMessageProcessing(object sender, StartedMessageProcessingEventArgs e)
        {
            _messageBeingHandled = e.Message;

            AddProcessingInformationHeaders(_messageBeingHandled);

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

        void AddProcessingInformationHeaders(TransportMessage message)
        {
            message.Headers[Headers.ProcessingEndpoint] = Configure.EndpointName;
            message.Headers[Headers.ProcessingMachine] = RuntimeEnvironment.MachineName;
        }


        /// <summary>
        /// Wraps the provided messages in an NServiceBus envelope, does not include destination.
        /// Invokes message mutators.
        /// </summary>
        /// <param name="rawMessages">The messages to wrap.</param>
        /// <param name="result">The envelope in which the messages are placed.</param>
        /// <returns>The envelope containing the messages.</returns>
        void MapTransportMessageFor(IList<object> rawMessages, TransportMessage result)
        {
            if (!Configure.SendOnlyMode)
            {
                result.ReplyToAddress = Address.Local;

                if (PropagateReturnAddressOnSend && _messageBeingHandled != null && _messageBeingHandled.ReplyToAddress != null)
                    result.ReplyToAddress = _messageBeingHandled.ReplyToAddress;
            }

            var messages = ApplyOutgoingMessageMutatorsTo(rawMessages).ToArray();


            var messageDefinitions = rawMessages.Select(m => MessageMetadataRegistry.GetMessageDefinition(GetMessageType(m))).ToList();

            result.TimeToBeReceived = messageDefinitions.Min(md => md.TimeToBeReceived);
            result.Recoverable = messageDefinitions.Any(md => md.Recoverable);

            SerializeMessages(result, messages);

            InvokeOutgoingTransportMessagesMutators(messages, result);
        }

        Type GetMessageType(object message)
        {
            var messageType = message.GetType();

            return MessageMapper.GetMappedTypeFor(messageType);
        }

        void SerializeMessages(TransportMessage result, object[] messages)
        {
            using (var ms = new MemoryStream())
            {
                MessageSerializer.Serialize(messages, ms);

                result.Headers[Headers.ContentType] = MessageSerializer.ContentType;

                if (messages.Any())
                    result.Headers[Headers.EnclosedMessageTypes] = SerializeEnclosedMessageTypes(messages);

                result.Body = ms.ToArray();
            }
        }

        string SerializeEnclosedMessageTypes(IEnumerable<object> messages)
        {
            var types = messages.Select(m => MessageMapper.GetMappedTypeFor(m.GetType())).ToList();

            var interfaces = types.SelectMany(t => t.GetInterfaces())
                .Where(MessageConventionExtensions.IsMessageType);

            var distinctTypes = types.Distinct();
            var interfacesOrderedByHierarchy = interfaces.Distinct().OrderByDescending(i => i.GetInterfaces().Count()); // Interfaces with less interfaces are lower in the hierarchy. 

            return string.Join(";", distinctTypes.Concat(interfacesOrderedByHierarchy).Select(t => t.AssemblyQualifiedName));
        }

        private void InvokeOutgoingTransportMessagesMutators(object[] messages, TransportMessage result)
        {
            var mutators = Builder.BuildAll<IMutateOutgoingTransportMessages>();
            if (mutators != null)
                foreach (var mutator in mutators)
                {
                    Log.DebugFormat("Invoking transport message mutator: {0}", mutator.GetType().FullName);
                    mutator.MutateOutgoing(messages, result);
                }
        }

        IEnumerable<object> ApplyOutgoingMessageMutatorsTo(IEnumerable<object> messages)
        {
            foreach (var originalMessage in messages)
            {
                var mutators = Builder.BuildAll<IMutateOutgoingMessages>().ToList();

                var mutatedMessage = originalMessage;
                mutators.ForEach(m =>
                {
                    mutatedMessage = m.MutateOutgoing(mutatedMessage);
                });

                yield return mutatedMessage;
            }
        }

        /// <summary>
        /// Uses the first message in the array to pass to <see cref="GetAddressForMessageType"/>.
        /// </summary>
        Address GetAddressForMessages(object[] messages)
        {
            if (messages == null || messages.Length == 0)
                return Address.Undefined;

            return GetAddressForMessageType(messages[0].GetType());
        }

        /// <summary>
        /// Gets the destination address For a message type.
        /// </summary>
        /// <param name="messageType">The message type to get the destination for.</param>
        /// <returns>The address of the destination associated with the message type.</returns>
        Address GetAddressForMessageType(Type messageType)
        {
            var destination = MessageRouter.GetDestinationFor(messageType);

            if (destination != Address.Undefined)
                return destination;


            if (messageMapper != null && !messageType.IsInterface)
            {
                var t = messageMapper.GetMappedTypeFor(messageType);
                if (t != null && t != messageType)
                    return GetAddressForMessageType(t);
            }


            return destination;
        }

        /// <summary>
        /// Throws an exception if the bus hasn't begun the startup process.
        /// </summary>
        protected void AssertBusIsStarted()
        {
            if (starting == false)
                throw new InvalidOperationException("The bus is not started yet, call Bus.Start() before attempting to use the bus.");
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

        /// <remarks>
        /// ThreadStatic
        /// </remarks>
        [ThreadStatic]
        static TransportMessage _messageBeingHandled;

        volatile bool started;
        volatile bool starting;
        object startLocker = new object();

        BehaviorContextStacker contextStacker = new BehaviorContextStacker();

        static ILog Log = LogManager.GetLogger(typeof(UnicastBus));

        IList<IWantToRunWhenBusStartsAndStops> thingsToRunAtStartup;

#pragma warning disable 3005
        protected ITransport transport;
#pragma warning restore 3005

        IMessageMapper messageMapper;
        Task[] thingsToRunAtStartupTask = new Task[0];
        SatelliteLauncher satelliteLauncher;
    }
}
