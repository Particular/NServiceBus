using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using Common.Logging;
using NServiceBus.MessageMutator;
using NServiceBus.Unicast.Queuing;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Transport;
using NServiceBus.ObjectBuilder;
using NServiceBus.MessageInterfaces;
using NServiceBus.Saga;
using System.Linq;
using NServiceBus.Serialization;
using System.IO;
using NServiceBus.Faults;
using NServiceBus.UnitOfWork;

namespace NServiceBus.Unicast
{
    using System.Diagnostics;

    /// <summary>
    /// A unicast implementation of <see cref="IBus"/> for NServiceBus.
    /// </summary>
    public class UnicastBus : IUnicastBus, IStartableBus
    {
        /// <summary>
        /// Header entry key for the given message type that is being subscribed to, when message intent is subscribe or unsubscribe.
        /// </summary>
        public const string SubscriptionMessageType = "SubscriptionMessageType";

        #region config properties

        private bool autoSubscribe = true;

        /// <summary>
        /// When set, when starting up, the bus performs 
        /// a subscribe operation for message types for which it has
        /// handlers and that are owned by a different endpoint.
        /// Default is true.
        /// </summary>
        public bool AutoSubscribe
        {
            get { return autoSubscribe; }
            set { autoSubscribe = value; }
        }

        /// <summary>
        /// True if the sagas shouldn't be autosubscribed
        /// </summary>
        public bool DoNotAutoSubscribeSagas { get; set; }

        private bool disableMessageHandling;

        /// <summary>
        /// Should be used by programmer, not administrator.
        /// Disables the handling of incoming messages.
        /// </summary>
        public virtual bool DisableMessageHandling
        {
            set { disableMessageHandling = value; }
        }

        /// <summary>
        /// A reference to the transport.
        /// </summary>
        protected ITransport transport;

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
        /// Information regarding the current TimeoutManager node
        /// </summary>
        public Address TimeoutManagerAddress { get; set; }

        /// <summary>
        /// Throttling message receiving speed according to NServiceBus licensing model.
        /// </summary>
        public int MaxThroughputPerSecond { get; set; }

        /// <summary>
        /// A delegate for a method that will handle the <see cref="MessageReceived"/>
        /// event.
        /// </summary>
        /// <param name="message">The message received.</param>
        public delegate void MessageReceivedDelegate(TransportMessage message);

        /// <summary>
        /// Event raised when a message is received.
        /// </summary>
        public event MessageReceivedDelegate MessageReceived;

        /// <summary>
        /// Event raised when messages are sent.
        /// </summary>
        public event EventHandler<MessagesEventArgs> MessagesSent;

        /// <summary>
        /// Clear Timeouts For the saga
        /// </summary>
        /// <param name="sagaId">Id of the Saga for clearing the timeouts</param>
        public void ClearTimeoutsFor(Guid sagaId)
        {
            var controlMessage = ControlMessage.Create();

            controlMessage.Headers[Headers.SagaId] = sagaId.ToString();
            controlMessage.Headers[Headers.ClearTimeouts] = true.ToString();

            MessageSender.Send(controlMessage, TimeoutManagerAddress);
        }

        /// <summary>
        /// Should be used by programmer, not administrator.
        /// Gets and sets an <see cref="ISubscriptionStorage"/> implementation to
        /// be used for subscription storage for the bus.
        /// </summary>
        public virtual ISubscriptionStorage SubscriptionStorage { get; set; }

        /// <summary>
        /// Should be used by the programmer, not the administrator.
        /// Gets and sets an <see cref="IMessageSerializer"/> implementation to
        /// be used for subscription storage for the bus.
        /// </summary>
        public virtual IMessageSerializer MessageSerializer { get; set; }

        /// <summary>
        /// Should be used by programmer, not administrator.
        /// Sets <see cref="IBuilder"/> implementation that will be used to 
        /// dynamically instantiate and execute message handlers.
        /// </summary>
        public IBuilder Builder { get; set; }

        private IMessageMapper messageMapper;

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
        /// should be propogated when the message is forwarded. This field is
        /// used primarily for the Distributor.
        /// </summary>
        public bool PropogateReturnAddressOnSend { get; set; }


        /// <summary>
        /// Should be used by administrator, not programmer.
        /// Sets the address to which all messages received on this bus will be 
        /// forwarded to (not including subscription messages). 
        /// This is primarily useful for smart client scenarios 
        /// where both client and server software are installed on the mobile
        /// device. The server software will have this field set to the address
        /// of the real server.
        /// </summary>
        public Address ForwardReceivedMessagesTo { get; set; }

        /// <summary>
        /// The TTR to set on forwarded messages. 
        /// </summary>
        public TimeSpan TimeToBeReceivedOnForwardedMessages { get; set; }


        /// <summary>
        /// Should be used by administrator, not programmer.
        /// Sets the message types associated with the bus.
        /// </summary>
        /// <remarks>
        /// This property accepts a dictionary where the key can be the name of a type implementing
        /// <see cref="IMessage"/> or the name of an assembly that contains message types.  The value 
        /// of each entry is the address of the owner of the message type defined in the key.
        /// If an assembly is specified then all the the types in the assembly implementing <see cref="IMessage"/> 
        /// will be registered against the address defined in the value of the entry.
        /// </remarks>
        public IDictionary<Type, Address> MessageOwners
        {
            set
            {
                value.ToList()
                    .ForEach((k) => RegisterMessageType(k.Key, k.Value));
            }
            get { return null; }
        }

        /// <summary>
        /// Sets the list of assemblies which contain a message handlers
        /// for the bus.
        /// </summary>
        public virtual IList MessageHandlerAssemblies
        {
            set
            {
                var types = new List<Type>();
                foreach (Assembly a in value)
                    types.AddRange(a.GetTypes());

                MessageHandlerTypes = types;
            }
        }

        /// <summary>
        /// Sets the types that will be scanned for message handlers.
        /// Those found will be invoked in the same order as given.
        /// </summary>
        public IEnumerable<Type> MessageHandlerTypes
        {
            get { return messageHandlerTypes; }
            set
            {
                messageHandlerTypes = value;

                foreach (Type t in value)
                    IfTypeIsMessageHandlerThenLoad(t);
            }
        }
        private IEnumerable<Type> messageHandlerTypes;

        /// <summary>
        /// Object that will be used to authorize subscription requests.
        /// </summary>
        public IAuthorizeSubscriptions SubscriptionAuthorizer { get; set; }

        /// <summary>
        /// Object that will be used to manage failures.
        /// </summary>
        public IManageMessageFailures FailureManager { get; set; }

        /// <summary>
        /// Gets or Set AllowSubscribeToSelf 
        /// </summary>
        public bool AllowSubscribeToSelf { get; set; }

        #endregion

        #region IUnicastBus Members

        /// <summary>
        /// Event raised when no subscribers found for the published message.
        /// </summary>
        public event EventHandler<MessageEventArgs> NoSubscribersForMessage;

        /// <summary>
        /// Event raised when client subscribed to a message type.
        /// </summary>
        public event EventHandler<SubscriptionEventArgs> ClientSubscribed;

        #endregion

        #region IBus Members

        /// <summary>
        /// Creates an instance of the specified type.
        /// Used primarily for instantiating interface-based messages.
        /// </summary>
        /// <typeparam name="T">The type to instantiate.</typeparam>
        /// <returns>An instance of the specified type.</returns>
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
        /// <returns></returns>
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
        public object CreateInstance(Type messageType)
        {
            return messageMapper.CreateInstance(messageType);
        }

        /// <summary>
        /// Creates an instance of the requested message type (T), 
        /// performing the given action on the created message,
        /// and then publishing it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageConstructor"></param>
        public void Publish<T>(Action<T> messageConstructor)
        {
            Publish(CreateInstance(messageConstructor));
        }

        /// <summary>
        /// Publishes the messages to all subscribers of the first message's type.
        /// </summary>
        /// <param name="messages"></param>
        public virtual void Publish<T>(params T[] messages)
        {
            if (SubscriptionStorage == null)
                throw new InvalidOperationException("Cannot publish on this endpoint - no subscription storage has been configured. Add either 'MsmqSubscriptionStorage()' or 'DbSubscriptionStorage()' after 'NServiceBus.Configure.With()'.");

            if (messages == null || messages.Length == 0) // Bus.Publish<IFoo>();
            {
                Publish(CreateInstance<T>(m => { }));
                return;
            }

            MessagingBestPractices.AssertIsValidForPubSub(messages[0].GetType());

            var fullTypes = GetFullTypes(messages as object[]);
            var subscribers = SubscriptionStorage.GetSubscriberAddressesForMessage(fullTypes.Select(t => new MessageType(t)))
                .ToList();

            if (!subscribers.Any())
                if (NoSubscribersForMessage != null)
                    NoSubscribersForMessage(this, new MessageEventArgs(messages[0]));

            SendMessage(subscribers, null, MessageIntentEnum.Publish, messages as object[]);
        }

        /// <summary>
        /// Subscribes to the given type - T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
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
        /// <typeparam name="T"></typeparam>
        /// <param name="condition"></param>
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
            AssertBusIsStarted();
            AssertHasLocalAddress();

            var destination = GetAddressForMessageType(messageType);
            if (Address.Self == destination)
                throw new InvalidOperationException(string.Format("Message {0} is owned by the same endpoint that you're trying to subscribe", messageType));

            if (destination == Address.Undefined)
                throw new InvalidOperationException(string.Format("No destination could be found for message type {0}. Check the <MessageEndpointMappings> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.", messageType));

            subscriptionsManager.AddConditionForSubscriptionToMessageType(messageType, condition);


            Log.Info("Subscribing to " + messageType.AssemblyQualifiedName + " at publisher queue " + destination);
            var subscriptionMessage = ControlMessage.Create();

            subscriptionMessage.Headers[SubscriptionMessageType] = messageType.AssemblyQualifiedName;
            subscriptionMessage.MessageIntent = MessageIntentEnum.Subscribe;
            InvokeOutgoingTransportMessagesMutators(new object[] { }, subscriptionMessage);

            ThreadPool.QueueUserWorkItem(state =>
                                         SendSubscribeMessageWithRetries(destination, subscriptionMessage, messageType.AssemblyQualifiedName));
        }

        /// <summary>
        /// Unsubscribes from the given type of message - T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Unsubscribe<T>()
        {
            Unsubscribe(typeof(T));
        }

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <param name="messageType"></param>
        public virtual void Unsubscribe(Type messageType)
        {
            MessagingBestPractices.AssertIsValidForPubSub(messageType);
            AssertBusIsStarted();
            AssertHasLocalAddress();

            var destination = GetAddressForMessageType(messageType);

            if (destination == Address.Undefined)
                throw new InvalidOperationException(string.Format("No destination could be found for message type {0}. Check the <MessageEndpointMapping> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.", messageType));

            Log.Info("Unsubscribing from " + messageType.AssemblyQualifiedName + " at publisher queue " + destination);

            var subscriptionMessage = ControlMessage.Create();

            subscriptionMessage.Headers[SubscriptionMessageType] = messageType.AssemblyQualifiedName;
            subscriptionMessage.MessageIntent = MessageIntentEnum.Unsubscribe;

            InvokeOutgoingTransportMessagesMutators(new object[] { }, subscriptionMessage);

            MessageSender.Send(subscriptionMessage, destination);
        }

        void SendSubscribeMessageWithRetries(Address destination, TransportMessage subscriptionMessage, string messageType, int retriesCount = 0)
        {
            try
            {
                MessageSender.Send(subscriptionMessage, destination);
            }
            catch (QueueNotFoundException ex)
            {
                if (retriesCount < 5)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                    SendSubscribeMessageWithRetries(destination, subscriptionMessage, messageType, ++retriesCount);
                }
                else
                {
                    Log.ErrorFormat("Failed to subscribe to {0} at publisher queue {1}", ex, messageType, destination);
                }
            }
        }

        void IBus.Reply(params object[] messages)
        {
            MessagingBestPractices.AssertIsValidForReply(messages.ToList());
            SendMessage(_messageBeingHandled.ReplyToAddress, _messageBeingHandled.IdForCorrelation, MessageIntentEnum.Send, messages);
        }

        void IBus.Reply<T>(Action<T> messageConstructor)
        {
            ((IBus)this).Reply(CreateInstance(messageConstructor));
        }

        void IBus.Return<T>(T errorCode)
        {
            var returnMessage = ControlMessage.Create();

            returnMessage.Headers[Headers.ReturnMessageErrorCodeHeader] = errorCode.GetHashCode().ToString();
            returnMessage.CorrelationId = _messageBeingHandled.IdForCorrelation;
            returnMessage.MessageIntent = MessageIntentEnum.Send;

            InvokeOutgoingTransportMessagesMutators(new object[] { }, returnMessage);

            MessageSender.Send(returnMessage, _messageBeingHandled.ReplyToAddress);
        }

        void IBus.HandleCurrentMessageLater()
        {
            if (_handleCurrentMessageLaterWasCalled)
                return;

            MessageSender.Send(_messageBeingHandled, Address.Local);

            _handleCurrentMessageLaterWasCalled = true;
        }

        void IBus.ForwardCurrentMessageTo(string destination)
        {
            MessageSender.Send(_messageBeingHandled, Address.Parse(destination));
        }

        /// <summary>
        /// ThreadStatic variable indicating if the current message was already
        /// marked to be handled later so we don't do this more than once.
        /// </summary>
        [ThreadStatic]
        private static bool _handleCurrentMessageLaterWasCalled;

        ICallback IBus.SendLocal<T>(Action<T> messageConstructor)
        {
            return ((IBus)this).SendLocal(CreateInstance(messageConstructor));
        }

        ICallback IBus.SendLocal(params object[] messages)
        {
            return ((IBus)this).Send(Address.Local, messages);
        }

        ICallback IBus.Send<T>(Action<T> messageConstructor)
        {
            return ((IBus)this).Send(CreateInstance(messageConstructor));
        }

        ICallback IBus.Send(params object[] messages)
        {
            var destination = GetAddressForMessages(messages);

            return SendMessage(destination, null, MessageIntentEnum.Send, messages);
        }

        ICallback IBus.Send<T>(string destination, Action<T> messageConstructor)
        {
            return SendMessage(destination, null, MessageIntentEnum.Send, CreateInstance(messageConstructor));
        }

        ICallback IBus.Send<T>(Address address, Action<T> messageConstructor)
        {
            return SendMessage(address, null, MessageIntentEnum.Send, CreateInstance(messageConstructor));
        }

        ICallback IBus.Send(string destination, params object[] messages)
        {
            return SendMessage(destination, null, MessageIntentEnum.Send, messages);
        }

        ICallback IBus.Send(Address address, params object[] messages)
        {
            return SendMessage(address, null, MessageIntentEnum.Send, messages);
        }

        ICallback IBus.Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            return SendMessage(destination, correlationId, MessageIntentEnum.Send, CreateInstance(messageConstructor));
        }

        ICallback IBus.Send<T>(Address address, string correlationId, Action<T> messageConstructor)
        {
            return SendMessage(address, correlationId, MessageIntentEnum.Send, CreateInstance(messageConstructor));
        }

        ICallback IBus.Send(string destination, string correlationId, params object[] messages)
        {
            return SendMessage(destination, correlationId, MessageIntentEnum.Send, messages);
        }

        ICallback IBus.Send(Address address, string correlationId, params object[] messages)
        {
            return SendMessage(address, correlationId, MessageIntentEnum.Send, messages);
        }

        ICallback IBus.SendToSites(IEnumerable<string> siteKeys, params object[] messages)
        {
            if (messages == null || messages.Length == 0)
                throw new InvalidOperationException("Cannot send an empty set of messages.");

            messages[0].SetDestinationSitesHeader(string.Join(",", siteKeys.ToArray()));

            return SendMessage(MasterNodeAddress.SubScope("gateway"), null, MessageIntentEnum.Send, messages);
        }

        /// <summary>
        /// Defer
        /// </summary>
        /// <param name="delay">Delay</param>
        /// <param name="messages">Messages</param>
        /// <returns></returns>
        public ICallback Defer(TimeSpan delay, params object[] messages)
        {
            return Defer(DateTime.UtcNow + delay, messages);
        }

        /// <summary>
        /// Defer
        /// </summary>
        /// <param name="processAt">processAt</param>
        /// <param name="messages">messages</param>
        /// <returns></returns>
        public ICallback Defer(DateTime processAt, params object[] messages)
        {
            try
            {
                messages.First().SetHeader(Headers.Expire, processAt.ToWireFormattedString());

                if (processAt.ToUniversalTime() <= DateTime.UtcNow)
                {
                    return ((IBus)this).SendLocal(messages);
                }

                return ((IBus)this).Send(TimeoutManagerAddress, messages);
            }
            catch (Exception)
            {
                Log.Error("It might be that TimeoutManager is not configured. Please configure .RunTimeoutManager() at your endpoint.");
                throw;
            }
        }

        private ICallback SendMessage(string destination, string correlationId, MessageIntentEnum messageIntent, params object[] messages)
        {
            if (destination == null)
            {
                var tm = messages[0] as TimeoutMessage;
                if (tm != null)
                    if (tm.ClearTimeout)
                        return null;
            }

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
                var result = new Callback(id);
                result.Registered += delegate(object sender, BusAsyncResultEventArgs args)
                {
                    lock (messageIdToAsyncResultLookup)
                        messageIdToAsyncResultLookup[args.MessageId] = args.Result;
                };

                return result;
            }

            return null;
        }

        private ICollection<string> SendMessage(List<Address> addresses, string correlationId, MessageIntentEnum messageIntent, params object[] messages)
        {
            messages.ToList()
                        .ForEach(message => MessagingBestPractices.AssertIsValidForSend(message.GetType(), messageIntent));

            if (messages.Length > 1)
            {
                // Users can't send more than one message with a DataBusProperty in the same TransportMessage, Yes this is a bug that will be fixed in v4!

                var numberOfMessagesWithDataBusProperties = 0;
                foreach (var message in messages)
                {
                    var hasAtLeastOneDataBusProperty = message.GetType().GetProperties().Any(p => p.IsDataBusProperty());

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

            var toSend = new TransportMessage { CorrelationId = correlationId, MessageIntent = messageIntent };

            MapTransportMessageFor(messages, toSend);

            foreach (var destination in addresses)
            {
                try
                {
                    MessageSender.Send(toSend, destination);
                }
                catch (QueueNotFoundException ex)
                {
                    throw new ConfigurationException("The destination queue '" + destination +
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



        static List<Type> GetFullTypes(IEnumerable<object> messages)
        {
            var types = new List<Type>();

            foreach (var m in messages)
            {
                var s = m.GetType();
                if (types.Contains(s))
                    continue;

                types.Add(s);

                foreach (var t in m.GetType().GetInterfaces())
                    if (t.IsMessageType())
                        if (!types.Contains(t))
                            types.Add(t);
            }

            return types;
        }

        /// <summary>
        /// Implementation of IStartableBus.Started event.
        /// </summary>
        public event EventHandler Started;

        IBus IStartableBus.Start()
        {
            return (this as IStartableBus).Start(null);
        }

        IBus IStartableBus.Start(Action startupAction)
        {
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
                    startupAction();

                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

                if (SubscriptionStorage != null)
                    SubscriptionStorage.Init();

                if (!DoNotStartTransport)
                {
                    transport.MaxThroughputPerSecond = MaxThroughputPerSecond;
                    transport.Start(InputAddress);
                }

                if (autoSubscribe)
                {
                    PerformAutoSubcribe();
                }

                started = true;
            }

            if (Started != null)
                Started(this, null);

            return this;
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


        void PerformAutoSubcribe()
        {
            AssertHasLocalAddress();

            foreach (var messageType in GetEventsToAutoSubscribe())
            {
                var otherHandlersThanSagas = GetHandlerTypes(messageType).Any(t => !typeof(ISaga).IsAssignableFrom(t));

                if (DoNotAutoSubscribeSagas && !otherHandlersThanSagas)
                {
                    Log.InfoFormat("Message type {0} is not auto subscribed since its only handled by sagas and auto subscription for sagas is currently turned off", messageType);
                    continue;
                }

                Subscribe(messageType);

                if (!messageType.IsEventType())
                    Log.Info("Future versions of NServiceBus will only autosubscribe messages explicitly marked as IEvent so consider marking messages that are events with the explicit IEvent interface");
            }
        }

        IEnumerable<Type> GetEventsToAutoSubscribe()
        {
            var eventsHandled = GetMessageTypesHandledOnThisEndpoint().Where(t => !t.IsCommandType()).ToList();

            if (AllowSubscribeToSelf)
                return eventsHandled;

            var eventsWithRouting = messageTypeToDestinationLookup
                .Where(route => route.Value != Address.Undefined &&
                                eventsHandled.Any(t => t.IsAssignableFrom(route.Key)))
                .Select(route => route.Key)
                .ToList();

            return eventsWithRouting;
        }

        void AssertHasLocalAddress()
        {
            if (Address.Local == null)
                throw new InvalidOperationException("Cannot start subscriber without a queue configured. Please specify the LocalAddress property of UnicastBusConfig.");
        }

        void ValidateConfiguration()
        {
            if (MessageSerializer == null)
                throw new InvalidOperationException("No message serializer has been configured.");
        }

        /// <summary>
        /// Tells the transport to dispose.
        /// </summary>
        public virtual void Dispose()
        {
            transport.StartedMessageProcessing -= TransportStartedMessageProcessing;
            transport.TransportMessageReceived -= TransportMessageReceived;
            transport.FinishedMessageProcessing -= TransportFinishedMessageProcessing;
            transport.FailedMessageProcessing -= TransportFailedMessageProcessing;

            transport.Dispose();
        }


        void IBus.DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            _doNotContinueDispatchingCurrentMessageToHandlers = true;
        }

        [ThreadStatic]
        private static bool _doNotContinueDispatchingCurrentMessageToHandlers;

        IDictionary<string, string> IBus.OutgoingHeaders
        {
            get
            {
                return ExtensionMethods.GetStaticOutgoingHeadersAction();
            }
        }

        IMessageContext IBus.CurrentMessageContext
        {
            get
            {
                return _messageBeingHandled == null ? null : new MessageContext(_messageBeingHandled);
            }
        }

        #endregion

        #region receiving and handling

        /// <summary>
        /// Handles a received message.
        /// </summary>
        /// <param name="builder">The builder used to construct the objects necessary to handle the message.</param>
        /// <param name="m">The received message.</param>
        /// <remarks>
        /// run by multiple threads so must be thread safe
        /// public for testing
        /// </remarks>
        public void HandleMessage(IBuilder builder, TransportMessage m)
        {
            var messages = new object[0];

            if (!m.IsControlMessage())
            {
                messages = Extract(m);

                if (messages == null || messages.Length == 0)
                {
                    Log.Warn("Received an empty message - ignoring.");
                    return;
                }
            }

            //apply mutators
            messages = messages.Select(msg =>
                                           {
                                               //message mutators may need to assume that this has been set (eg. for the purposes of headers).
                                               ExtensionMethods.CurrentMessageBeingHandled = msg;

                                               return ApplyIncomingMessageMutatorsTo(builder, msg);
                                           }).ToArray();

            HandleCorellatedMessage(m, messages);

            foreach (var messageToHandle in messages)
            {
                ExtensionMethods.CurrentMessageBeingHandled = messageToHandle;

                var canDispatch = true;
                foreach (var condition in subscriptionsManager.GetConditionsForMessage(messageToHandle))
                {
                    if (condition(messageToHandle)) continue;

                    Log.Debug(string.Format("Condition {0} failed for message {1}", condition, messageToHandle.GetType().Name));
                    canDispatch = false;
                    break;
                }

                if (canDispatch)
                {
                    var handlers = DispatchMessageToHandlersBasedOnType(builder, messageToHandle).ToList();

                    if (!handlers.Any())
                    {
                        var warning = string.Format("No handlers could be found for message type: {0}", messageToHandle);

                        if (Debugger.IsAttached)
                            throw new InvalidOperationException(warning);

                        Log.WarnFormat(warning);
                    }

                    LogPipelineInfo(messageToHandle, handlers);
                }
            }
            ExtensionMethods.CurrentMessageBeingHandled = null;
        }


        static object ApplyIncomingMessageMutatorsTo(IBuilder builder, object originalMessage)
        {
            var mutators = builder.BuildAll<IMutateIncomingMessages>();

            var mutatedMessage = originalMessage;
            mutators.ToList().ForEach(m =>
            {
                mutatedMessage = m.MutateIncoming(mutatedMessage);
            });

            return mutatedMessage;
        }

        private object[] Extract(TransportMessage m)
        {
            try
            {
                if (m.Body == null || m.Body.Length == 0)
                    return null;

                return MessageSerializer.Deserialize(new MemoryStream(m.Body));
            }
            catch (Exception e)
            {
                try
                {
                    FailureManager.SerializationFailedForMessage(m, e);
                }
                catch (Exception exception)
                {
                    Configure.Instance.OnCriticalError();
                }

                return null;
            }
        }

        /// <summary>
        /// Finds the message handlers associated with the message type and dispatches
        /// the message to the found handlers.
        /// </summary>
        /// <param name="builder">The builder used to construct the handlers.</param>
        /// <param name="toHandle">The message to dispatch to the handlers.</param>
        /// <param name="messageType">The message type by which to locate the correct handlers.</param>
        /// <returns></returns>
        /// <remarks>
        /// If during the dispatch, a message handler calls the DoNotContinueDispatchingCurrentMessageToHandlers method,
        /// this prevents the message from being further dispatched.
        /// This includes generic message handlers (of IMessage), and handlers for the specific messageType.
        /// </remarks>
        IEnumerable<Type> DispatchMessageToHandlersBasedOnType(IBuilder builder, object toHandle)
        {
            var messageType = toHandle.GetType();
            var invokedHandlers = new List<Type>();

            foreach (var handlerType in GetHandlerTypes(messageType))
            {
                try
                {
                    var handlerTypeToInvoke = handlerType;

                    var factory = GetDispatcherFactoryFor(handlerTypeToInvoke, builder);

                    var dispatchers = factory.GetDispatcher(handlerTypeToInvoke, builder, toHandle).ToList();

                    dispatchers.ForEach(dispatch =>
                                            {
                                                Log.DebugFormat("Dispatching message {0} to handler{1}", messageType, handlerTypeToInvoke);
                                                dispatch();
                                            });

                    invokedHandlers.Add(handlerTypeToInvoke);
                    if (_doNotContinueDispatchingCurrentMessageToHandlers)
                    {
                        _doNotContinueDispatchingCurrentMessageToHandlers = false;
                        break;
                    }
                }
                catch (Exception e)
                {
                    var innerEx = GetInnermostException(e);
                    Log.Warn(handlerType.Name + " failed handling message.", GetInnermostException(innerEx));

                    throw new TransportMessageHandlingFailedException(innerEx);
                }
            }
            return invokedHandlers;
        }

        IMessageDispatcherFactory GetDispatcherFactoryFor(Type messageHandlerTypeToInvoke, IBuilder builder)
        {
            Type factoryType;

            MessageDispatcherMappings.TryGetValue(messageHandlerTypeToInvoke, out factoryType);

            if (factoryType == null)
                throw new InvalidOperationException("No dispatcher factory type configured for messagehandler " + messageHandlerTypeToInvoke);

            var factory = builder.Build(factoryType) as IMessageDispatcherFactory;

            if (factory == null)
                throw new InvalidOperationException(string.Format("Registered dispatcher factory {0} for type {1} does not implement IMessageDispatcherFactory", factoryType, messageHandlerTypeToInvoke));

            return factory;
        }

        /// <summary>
        /// The list of message dispatcher factories to use
        /// </summary>
        public IDictionary<Type, Type> MessageDispatcherMappings { get; set; }


        /// <summary>
        /// Gets the inner most exception from an exception stack.
        /// </summary>
        /// <param name="e">The exception to get the inner most exception for.</param>
        /// <returns>The innermost exception.</returns>
        /// <remarks>
        /// If the exception has no inner exceptions the provided exception will be returned.
        /// </remarks>
        private static Exception GetInnermostException(Exception e)
        {
            if (e.InnerException == null)
                return e;

            var result = e;

            do
            {
                if (!result.Source.ToLower().Equals("mscorlib"))
                    return result;

                result = result.InnerException;

            } while (result.InnerException != null);

            return result;
        }

        /// <summary>
        /// If the message contains a correlationId, attempts to
        /// invoke callbacks for that Id.
        /// </summary>
        /// <param name="msg">The message to evaluate.</param>
        /// <param name="messages">The logical messages in the transport message.</param>
        void HandleCorellatedMessage(TransportMessage msg, object[] messages)
        {
            if (msg.CorrelationId == null)
                return;

            BusAsyncResult busAsyncResult;

            lock (messageIdToAsyncResultLookup)
            {
                messageIdToAsyncResultLookup.TryGetValue(msg.CorrelationId, out busAsyncResult);
                messageIdToAsyncResultLookup.Remove(msg.CorrelationId);
            }

            if (busAsyncResult == null)
                return;

            var statusCode = int.MinValue;

            if (msg.IsControlMessage() && msg.Headers.ContainsKey(Headers.ReturnMessageErrorCodeHeader))
                statusCode = int.Parse(msg.Headers[Headers.ReturnMessageErrorCodeHeader]);

            busAsyncResult.Complete(statusCode, messages);
        }

        /// <summary>
        /// Handles the <see cref="ITransport.TransportMessageReceived"/> event from the <see cref="ITransport"/> used
        /// for the bus.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The arguments for the event.</param>
        /// <remarks>
        /// When the transport passes up the <see cref="TransportMessage"/> its received,
        /// the bus checks for initializiation, 
        /// sets the message as that which is currently being handled for the current thread
        /// and, depending on <see cref="DisableMessageHandling"/>, attempts to handle the message.
        /// </remarks>
        private void TransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            using (var child = Builder.CreateChildBuilder())
                HandleTransportMessage(child, e.Message);
        }
        
        private void HandleTransportMessage(IBuilder childBuilder, TransportMessage msg)
        {
            Log.Debug("Received message with ID " + msg.Id + " from sender " + msg.ReplyToAddress);

            var unitsOfWork = childBuilder.BuildAll<IManageUnitsOfWork>().ToList();
            var unitsOfWorkStarted = new List<IManageUnitsOfWork>();
            var lastUnitOfWorkThatEndWasInvokedOnIndex = 0;

            try
            {
                foreach (var uow in unitsOfWork)
                {
                    unitsOfWorkStarted.Add(uow);
                    uow.Begin();
                }

                var transportMutators = childBuilder.BuildAll<IMutateIncomingTransportMessages>();
                if (transportMutators != null)
                    foreach (var mutator in transportMutators)
                        mutator.MutateIncoming(msg);

                if (HandledSubscriptionMessage(msg, SubscriptionStorage, SubscriptionAuthorizer))
                {
                    var messageType = GetSubscriptionMessageTypeFrom(msg);

                    if (msg.MessageIntent == MessageIntentEnum.Subscribe)
                        if (ClientSubscribed != null)
                            ClientSubscribed(this,
                                             new SubscriptionEventArgs
                                                 {
                                                     MessageType = messageType,
                                                     SubscriberReturnAddress = msg.ReplyToAddress
                                                 });

                    for (lastUnitOfWorkThatEndWasInvokedOnIndex = 0; lastUnitOfWorkThatEndWasInvokedOnIndex < unitsOfWorkStarted.Count; )
                    {
                        var uow = unitsOfWorkStarted[unitsOfWorkStarted.Count - 1 - lastUnitOfWorkThatEndWasInvokedOnIndex++];
                        uow.End();
                    }
                    return;
                }

                _handleCurrentMessageLaterWasCalled = false;

                if (MessageReceived != null)
                    MessageReceived(msg);

                if (!disableMessageHandling)
                    HandleMessage(childBuilder, msg);

                for (lastUnitOfWorkThatEndWasInvokedOnIndex = 0; lastUnitOfWorkThatEndWasInvokedOnIndex < unitsOfWorkStarted.Count;)
                {
                    var uow = unitsOfWorkStarted[unitsOfWorkStarted.Count - 1 - lastUnitOfWorkThatEndWasInvokedOnIndex++];
                    uow.End();
                }

                ForwardMessageIfNecessary(msg);
            }
            catch (Exception ex)
            {
                var exceptionsToThrow = new List<Exception> {ex};

                for (; lastUnitOfWorkThatEndWasInvokedOnIndex < unitsOfWorkStarted.Count; lastUnitOfWorkThatEndWasInvokedOnIndex++)
                {
                    var uow = unitsOfWorkStarted[unitsOfWorkStarted.Count - 1 - lastUnitOfWorkThatEndWasInvokedOnIndex];
                    try
                    {
                        uow.End(ex);
                    }
                    catch (Exception anotherException)
                    {
                        exceptionsToThrow.Add(anotherException);
                    }
                }

                if (exceptionsToThrow.Count == 1)
                {
                    throw exceptionsToThrow.First();
                }

                throw new AggregateException(exceptionsToThrow);
            }

            Log.Debug("Finished handling message.");
        }

        static string GetSubscriptionMessageTypeFrom(TransportMessage msg)
        {
            return (from header in msg.Headers where header.Key == SubscriptionMessageType select header.Value).FirstOrDefault();
        }

        /// <summary>
        /// Handles subscribe and unsubscribe requests managing the given subscription storage.
        /// Returns true if the message was a subscription message.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="subscriptionStorage"></param>
        /// <param name="subscriptionAuthorizer"></param>
        /// <returns></returns>
        public static bool HandledSubscriptionMessage(TransportMessage msg, ISubscriptionStorage subscriptionStorage, IAuthorizeSubscriptions subscriptionAuthorizer)
        {
            string messageTypeString = GetSubscriptionMessageTypeFrom(msg);

            Action warn = () =>
                              {
                                  var warning = string.Format("Subscription message from {0} arrived at this endpoint, yet this endpoint is not configured to be a publisher.", msg.ReplyToAddress);

                                  Log.Warn(warning);

                                  if (Debugger.IsAttached) // only under debug, so that we don't expose ourselves to a denial of service
                                      throw new InvalidOperationException(warning);  // and cause message to go to error queue by throwing an exception
                              };

            if (msg.MessageIntent == MessageIntentEnum.Subscribe)
                if (subscriptionStorage != null)
                {
                    bool goAhead = true;
                    if (subscriptionAuthorizer != null)
                        if (!subscriptionAuthorizer.AuthorizeSubscribe(messageTypeString, msg.ReplyToAddress.ToString(), msg.Headers))
                        {
                            goAhead = false;
                            Log.Debug(string.Format("Subscription request from {0} on message type {1} was refused.", msg.ReplyToAddress, messageTypeString));
                        }

                    if (goAhead)
                    {
                        Log.Info("Subscribing " + msg.ReplyToAddress + " to message type " + messageTypeString);
                        subscriptionStorage.Subscribe(msg.ReplyToAddress, new[] { new MessageType(messageTypeString) });
                    }

                    return true;
                }
                else
                {
                    warn();
                }

            if (msg.MessageIntent == MessageIntentEnum.Unsubscribe)
                if (subscriptionStorage != null)
                {
                    bool goAhead = true;

                    if (subscriptionAuthorizer != null)
                        if (!subscriptionAuthorizer.AuthorizeUnsubscribe(messageTypeString, msg.ReplyToAddress.ToString(), msg.Headers))
                        {
                            goAhead = false;
                            Log.Debug(string.Format("Unsubscribe request from {0} on message type {1} was refused.", msg.ReplyToAddress, messageTypeString));
                        }

                    if (goAhead)
                    {
                        Log.Info("Unsubscribing " + msg.ReplyToAddress + " from message type " + messageTypeString);
                        subscriptionStorage.Unsubscribe(msg.ReplyToAddress, new[] { new MessageType(messageTypeString) });
                    }

                    return true;
                }
                else
                {
                    warn();
                }

            return false;
        }

        void TransportFinishedMessageProcessing(object sender, EventArgs e)
        {
            modules.ForEach(module =>
            {
                Log.Debug("Calling 'HandleEndMessage' on " + module.GetType().FullName);
                module.HandleEndMessage();
            });
        }

        void TransportFailedMessageProcessing(object sender, FailedMessageProcessingEventArgs e)
        {
            modules.ForEach(module =>
            {
                Log.Debug("Calling 'HandleError' on " + module.GetType().FullName);
                module.HandleError();
            });
        }

        void TransportStartedMessageProcessing(object sender, StartedMessageProcessingEventArgs e)
        {
            _messageBeingHandled = e.Message;

            modules = Builder.BuildAll<IMessageModule>().ToList();

            modules.ForEach(module =>
            {
                Log.Debug("Calling 'HandleBeginMessage' on " + module.GetType().FullName);
                module.HandleBeginMessage(); //don't need to call others if one fails                                    
            });

            modules.Reverse();//make sure that the modules are called in reverse order when processing ends
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Sends the Msg to the address found in the field <see cref="ForwardReceivedMessagesTo"/>
        /// if it isn't null.
        /// </summary>
        /// <param name="m">The message to forward</param>
        private void ForwardMessageIfNecessary(TransportMessage m)
        {
            if (ForwardReceivedMessagesTo == null || ForwardReceivedMessagesTo == Address.Undefined)
                return;

            var toSend = new TransportMessage
                             {
                                 Body = m.Body,
                                 CorrelationId = m.CorrelationId,
                                 Headers = m.Headers,
                                 Id = m.Id,
                                 IdForCorrelation = m.IdForCorrelation,
                                 MessageIntent = m.MessageIntent,
                                 Recoverable = m.Recoverable,
                                 ReplyToAddress = Address.Local,
                                 TimeToBeReceived = TimeToBeReceivedOnForwardedMessages == TimeSpan.Zero ? m.TimeToBeReceived : TimeToBeReceivedOnForwardedMessages
                             };
            toSend.Headers["NServiceBus.OriginatingAddress"] = m.ReplyToAddress.ToString();

            MessageSender.Send(toSend, ForwardReceivedMessagesTo);
        }

        /// <summary>
        /// Registers a message type to a destination.
        /// </summary>
        /// <param name="messageType">A message type implementing <see cref="IMessage"/>.</param>
        /// <param name="address">The address of the destination the message type is registered to.</param>
        public void RegisterMessageType(Type messageType, Address address)
        {
            messageTypeToDestinationLocker.EnterWriteLock();
            messageTypeToDestinationLookup[messageType] = address;
            messageTypeToDestinationLocker.ExitWriteLock();

            if(!string.IsNullOrWhiteSpace(address.Machine))
                Log.Debug("Message " + messageType.FullName + " has been allocated to endpoint " + address + ".");

            if (!MessageConventionExtensions.IsExpressMessageType(messageType))
            {
                recoverableMessageTypes.Add(messageType);
            }

            var timeToBeReceived = MessageConventionExtensions.TimeToBeReceivedAction(messageType);

            if (timeToBeReceived == TimeSpan.MaxValue)
            {
                return;
            }

            timeToBeReceivedPerMessageType[messageType] = timeToBeReceived;
        }

        /// <summary>
        /// Wraps the provided messages in an NServiceBus envelope, does not include destination.
        /// Invokes message mutators.
        /// </summary>
        /// <param name="rawMessages">The messages to wrap.</param>
        /// /// <param name="result">The envelope in which the messages are placed.</param>
        /// <returns>The envelope containing the messages.</returns>
        protected TransportMessage MapTransportMessageFor(object[] rawMessages, TransportMessage result)
        {
            result.Headers = new Dictionary<string, string>();
            result.ReplyToAddress = Address.Local;

            var messages = ApplyOutgoingMessageMutatorsTo(rawMessages).ToArray();

            var ms = new MemoryStream();
            MessageSerializer.Serialize(messages, ms);
            result.Body = ms.ToArray();

            InvokeOutgoingTransportMessagesMutators(messages, result);

            if (PropogateReturnAddressOnSend)
                result.ReplyToAddress = Address.Local;

            var timeToBeReceived = TimeSpan.MaxValue;

            foreach (var message in messages)
            {
                var messageType = message.GetType();

                if (recoverableMessageTypes.Any(rt => rt.IsAssignableFrom(messageType)))
                    result.Recoverable = true;

                var span = GetTimeToBeReceivedForMessageType(messageType);
                timeToBeReceived = (span < timeToBeReceived ? span : timeToBeReceived);
            }

            result.TimeToBeReceived = timeToBeReceived;

            return result;
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
            var mutators = Builder.BuildAll<IMutateOutgoingMessages>();

            foreach (var originalMessage in messages)
            {

                var mutatedMessage = originalMessage;
                mutators.ToList().ForEach(m =>
                {
                    mutatedMessage = m.MutateOutgoing(mutatedMessage);
                });

                yield return mutatedMessage;
            }
        }

        private TimeSpan GetTimeToBeReceivedForMessageType(Type messageType)
        {
            var result = TimeSpan.MaxValue;

            timeToBeReceivedPerMessageTypeLocker.EnterReadLock();
            if (timeToBeReceivedPerMessageType.ContainsKey(messageType))
            {
                result = timeToBeReceivedPerMessageType[messageType];
                timeToBeReceivedPerMessageTypeLocker.ExitReadLock();
                return result;
            }

            var options = new List<TimeSpan>();
            foreach (var interfaceType in messageType.GetInterfaces())
            {
                if (timeToBeReceivedPerMessageType.ContainsKey(interfaceType))
                    options.Add(timeToBeReceivedPerMessageType[interfaceType]);
            }

            timeToBeReceivedPerMessageTypeLocker.ExitReadLock();

            if (options.Count > 0)
                result = options.Min();

            timeToBeReceivedPerMessageTypeLocker.EnterWriteLock();
            timeToBeReceivedPerMessageType[messageType] = result;
            timeToBeReceivedPerMessageTypeLocker.ExitWriteLock();

            return result;
        }

        /// <summary>
        /// Evaluates a type and loads it if it implements IMessageHander{T}.
        /// </summary>
        /// <param name="handler">The type to evaluate.</param>
        void IfTypeIsMessageHandlerThenLoad(Type handler)
        {
            if (handler.IsAbstract)
                return;


            foreach (var messageType in GetMessageTypesIfIsMessageHandler(handler))
            {
                if (!handlerList.ContainsKey(handler))
                    handlerList.Add(handler, new List<Type>());

                if (!(handlerList[handler].Contains(messageType)))
                {
                    handlerList[handler].Add(messageType);
                    Log.DebugFormat("Associated '{0}' message with '{1}' handler", messageType, handler);
                }

                HandlerInvocationCache.CacheMethodForHandler(handler, messageType);
            }
        }


        /// <summary>
        /// If the type is a message handler, returns all the message types that it handles
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static IEnumerable<Type> GetMessageTypesIfIsMessageHandler(Type type)
        {
            foreach (var t in type.GetInterfaces().Where(t => t.IsGenericType))
            {
                var potentialMessageType = t.GetGenericArguments().SingleOrDefault();

                if (potentialMessageType == null)
                    continue;

                if (potentialMessageType.IsMessageType() ||
                    typeof(IMessageHandler<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t))
                    yield return potentialMessageType;
            }
        }

        /// <summary>
        /// Gets a list of handler types associated with a message type.
        /// </summary>
        /// <param name="messageType">The type of message to get the handlers for.</param>
        /// <returns>The list of handler types associated with the message type.</returns>
        private IEnumerable<Type> GetHandlerTypes(Type messageType)
        {
            foreach (var handlerType in handlerList.Keys)
                foreach (var msgTypeHandled in handlerList[handlerType])
                    if (msgTypeHandled.IsAssignableFrom(messageType))
                    {
                        yield return handlerType;
                        break;
                    }
        }

        /// <summary>
        /// Returns all the message types which have handlers configured for them.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Type> GetMessageTypesHandledOnThisEndpoint()
        {
            foreach (var handlerType in handlerList.Keys)
                foreach (var typeHandled in handlerList[handlerType])
                    if (typeHandled.IsMessageType())
                        yield return typeHandled;
        }

        /// <summary>
        /// Uses the first message in the array to pass to <see cref="GetAddressForMessageType"/>.
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        Address GetAddressForMessages(object[] messages)
        {
            if (messages == null || messages.Length == 0)
                return Address.Undefined;


            return GetAddressForMessageType(messages[0].GetType());
        }

        /// <summary>
        /// Gets the destination address for a message type.
        /// </summary>
        /// <param name="messageType">The message type to get the destination for.</param>
        /// <returns>The address of the destination associated with the message type.</returns>
        Address GetAddressForMessageType(Type messageType)
        {
            Address destination;

            messageTypeToDestinationLocker.EnterReadLock();
            messageTypeToDestinationLookup.TryGetValue(messageType, out destination);
            messageTypeToDestinationLocker.ExitReadLock();

            if (destination == null)
                destination = Address.Undefined;

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

        void LogPipelineInfo(object messageToHandle, IEnumerable<Type> handlers)
        {
            var messageType = messageToHandle.GetType();

            _messageBeingHandled.Headers["NServiceBus.PipelineInfo." + messageType.FullName] = string.Join(";", handlers.Select(t => t.AssemblyQualifiedName));
        }


        #endregion

        #region Fields

        Address inputAddress;

        /// <summary>
        /// Gets/sets the subscription manager to use for the bus.
        /// </summary>
        protected SubscriptionsManager subscriptionsManager = new SubscriptionsManager();

        /// <summary>
        /// Thread-static list of message modules, needs to be initialized for every transport message
        /// </summary>
        [ThreadStatic]
        static List<IMessageModule> modules;

        /// <summary>
        /// Map of message IDs to Async Results - useful for cleanup in case of timeouts.
        /// </summary>
        protected readonly IDictionary<string, BusAsyncResult> messageIdToAsyncResultLookup = new Dictionary<string, BusAsyncResult>();

        private readonly IDictionary<Type, List<Type>> handlerList = new Dictionary<Type, List<Type>>();
        private readonly IList<Type> recoverableMessageTypes = new List<Type>();

        private readonly IDictionary<Type, TimeSpan> timeToBeReceivedPerMessageType = new Dictionary<Type, TimeSpan>();
        private readonly ReaderWriterLockSlim timeToBeReceivedPerMessageTypeLocker = new ReaderWriterLockSlim();

        /// <remarks>
        /// Accessed by multiple threads - needs appropriate locking
        /// </remarks>
        private readonly IDictionary<Type, Address> messageTypeToDestinationLookup = new Dictionary<Type, Address>();
        private readonly ReaderWriterLockSlim messageTypeToDestinationLocker = new ReaderWriterLockSlim();

        /// <remarks>
        /// ThreadStatic
        /// </remarks>
        [ThreadStatic]
        static TransportMessage _messageBeingHandled;

        private volatile bool started;
        private volatile bool starting;
        private readonly object startLocker = new object();

        private readonly static ILog Log = LogManager.GetLogger(typeof(UnicastBus));


        #endregion
    }


    /// <summary>
    /// Extansion methods for IBuilder
    /// </summary>
    public static class BuilderExtensions
    {
        /// <summary>
        /// Applies the action on the instances of T
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="action"></param>
        /// <typeparam name="T"></typeparam>
        public static void ForEach<T>(this IBuilder builder, Action<T> action)
        {
            var objs = builder.BuildAll<T>().ToList();

            objs.ForEach(action);
        }
    }
}