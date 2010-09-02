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
using System.Text;
using System.Linq;
using NServiceBus.Serialization;
using System.IO;
using NServiceBus.Faults;
using System.Net;
using NServiceBus.UnitOfWork;

namespace NServiceBus.Unicast
{
    /// <summary>
    /// A unicast implementation of <see cref="IBus"/> for NServiceBus.
    /// </summary>
    public class UnicastBus : IUnicastBus, IStartableBus
    {
        /// <summary>
        /// Header entry key for the given message type that is being subscribed to, when message intent is subscribe or unsubscribe.
        /// </summary>
        public const string SubscriptionMessageType = "SubscriptionMessageType";

        /// <summary>
        /// Header entry key indicating the types of messages contained.
        /// </summary>
        public const string EnclosedMessageTypes = "EnclosedMessageTypes";

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
        /// The address that the bus will listen to for messages.
        /// </summary>
        public string Address { get; set; }

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
        public IMessageQueue MessageSender { get; set; }

        /// <summary>
        /// Object used to manage units of work.
        /// </summary>
        public IManageUnitsOfWork UnitOfWorkManager { get; set; }

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
        public string ForwardReceivedMessagesTo { get; set; }

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
        public IDictionary MessageOwners
        {
            get { return messageOwners; }
            set
            {
                messageOwners = value;
                ConfigureMessageOwners(value);
            }
        }
        private IDictionary messageOwners;

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
        public T CreateInstance<T>() where T : IMessage
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
        public T CreateInstance<T>(Action<T> action) where T : IMessage
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
        public void Publish<T>(Action<T> messageConstructor) where T : IMessage
        {
            Publish(CreateInstance(messageConstructor));
        }

        /// <summary>
        /// Publishes the messages to all subscribers of the first message's type.
        /// </summary>
        /// <param name="messages"></param>
        public virtual void Publish<T>(params T[] messages) where T : IMessage
        {
            if (SubscriptionStorage == null)
                throw new InvalidOperationException("Cannot publish on this endpoint - no subscription storage has been configured. Add either 'MsmqSubscriptionStorage()' or 'DbSubscriptionStorage()' after 'NServiceBus.Configure.With()'.");

            var subscribers = SubscriptionStorage.GetSubscribersForMessage(GetFullTypes(messages as IMessage[]));

            if (subscribers.Count() == 0)
                if (NoSubscribersForMessage != null)
                    NoSubscribersForMessage(this, new MessageEventArgs(messages[0]));

            SendMessage(subscribers, null, MessageIntentEnum.Publish, messages as IMessage[]);
        }

        /// <summary>
        /// Subscribes to the given type - T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Subscribe<T>() where T : IMessage
        {
            Subscribe(typeof(T));
        }

        /// <summary>
        /// Subcribes to recieve published messages of the specified type.
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
        public void Subscribe<T>(Predicate<T> condition) where T : IMessage
        {
            var p = new Predicate<IMessage>(m =>
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
        public virtual void Subscribe(Type messageType, Predicate<IMessage> condition)
        {
            AssertBusIsStarted();

            subscriptionsManager.AddConditionForSubscriptionToMessageType(messageType, condition);

            var destination = GetDestinationForMessageType(messageType);

            if (destination == null)
                throw new InvalidOperationException(string.Format("No destination could be found for message type {0}. Check the <MessageEndpointMapping> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.", messageType));

            Log.Info("Subscribing to " + messageType.AssemblyQualifiedName + " at publisher queue " + destination);

            ((IBus)this).OutgoingHeaders[SubscriptionMessageType] = messageType.AssemblyQualifiedName;
            SendMessage(destination, null, MessageIntentEnum.Subscribe, new CompletionMessage());
            ((IBus)this).OutgoingHeaders.Remove(SubscriptionMessageType);
        }

        /// <summary>
        /// Unsubscribes from the given type of message - T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Unsubscribe<T>() where T : IMessage
        {
            Unsubscribe(typeof(T));
        }

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <param name="messageType"></param>
        public virtual void Unsubscribe(Type messageType)
        {
            var destination = GetDestinationForMessageType(messageType);

            if (destination == null)
                throw new InvalidOperationException(string.Format("No destination could be found for message type {0}. Check the <MessageEndpointMapping> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.", messageType));

            Log.Info("Unsubscribing from " + messageType.AssemblyQualifiedName + " at publisher queue " + destination);

            ((IBus)this).OutgoingHeaders[SubscriptionMessageType] = messageType.AssemblyQualifiedName;
            SendMessage(destination, null, MessageIntentEnum.Unsubscribe, new CompletionMessage());
            ((IBus)this).OutgoingHeaders.Remove(SubscriptionMessageType);
        }

        void IBus.Reply(params IMessage[] messages)
        {
            SendMessage(_messageBeingHandled.ReturnAddress, _messageBeingHandled.IdForCorrelation, MessageIntentEnum.Send, messages);
        }

        void IBus.Reply<T>(Action<T> messageConstructor)
        {
            ((IBus)this).Reply(CreateInstance(messageConstructor));
        }

        void IBus.Return<T>(T errorCode)
        {
            ((IBus)this).Reply(new CompletionMessage { ErrorCode = errorCode.GetHashCode() });
        }

        void IBus.HandleCurrentMessageLater()
        {
            if (_handleCurrentMessageLaterWasCalled)
                return;

            MessageSender.Send(_messageBeingHandled, Address);

            _handleCurrentMessageLaterWasCalled = true;
        }

        void IBus.ForwardCurrentMessageTo(string destination)
        {
            MessageSender.Send(_messageBeingHandled, destination);
        }

        /// <summary>
        /// ThreadStatic variable indicating if the current message was already
        /// marked to be handled later so we don't do this more than once.
        /// </summary>
        [ThreadStatic]
        private static bool _handleCurrentMessageLaterWasCalled;

        void IBus.SendLocal<T>(Action<T> messageConstructor)
        {
            SendLocal(CreateInstance(messageConstructor));
        }

        /// <summary>
        /// Sends the list of messages back to the current bus.
        /// </summary>
        /// <param name="messages">The messages to send.</param>
        public void SendLocal(params IMessage[] messages)
        {
            var m = GetTransportMessageFor(messages);
            
            m.MessageIntent = MessageIntentEnum.Send;

            MessageSender.Send(m, Address);
        }

        ICallback IBus.Send<T>(Action<T> messageConstructor)
        {
            return ((IBus)this).Send(CreateInstance(messageConstructor));
        }

        ICallback IBus.Send(params IMessage[] messages)
        {
            var destination = GetDestinationForMessages(messages);

            return SendMessage(destination, null, MessageIntentEnum.Send, messages);
        }

        ICallback IBus.Send<T>(string destination, Action<T> messageConstructor)
        {
            return SendMessage(destination, null, MessageIntentEnum.Send, CreateInstance(messageConstructor));
        }

        ICallback IBus.Send(string destination, params IMessage[] messages)
        {
            return SendMessage(destination, null, MessageIntentEnum.Send, messages);
        }

        void IBus.Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            SendMessage(destination, correlationId, MessageIntentEnum.Send, CreateInstance(messageConstructor));
        }

        void IBus.Send(string destination, string correlationId, params IMessage[] messages)
        {
            SendMessage(destination, correlationId, MessageIntentEnum.Send, messages);
        }


        private ICallback SendMessage(string destination, string correlationId, MessageIntentEnum messageIntent, params IMessage[] messages)
        {
            if (destination == null)
            {
                var tm = messages[0] as TimeoutMessage;
                if (tm != null)
                    if (tm.ClearTimeout)
                        return null;
            }

            if (destination == null)
                throw new InvalidOperationException(
                    string.Format("No destination specified for message {0}. Message cannot be sent. Check the UnicastBusConfig section in your config file and ensure that a MessageEndpointMapping exists for the message type.", messages[0].GetType().FullName));

            if (messages == null || messages.Length == 0)
                throw new InvalidOperationException("Cannot send an empty set of messages.");

            foreach (var id in SendMessage(new List<string> { destination }, correlationId, messageIntent, messages))
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

        private ICollection<string> SendMessage(IEnumerable<string> destinations, string correlationId, MessageIntentEnum messageIntent, params IMessage[] messages)
        {
            AssertBusIsStarted();

            var result = new List<string>();

            messages[0].SetHeader(EnclosedMessageTypes, SerializeEnclosedMessageTypes(messages));
            var toSend = GetTransportMessageFor(messages);

            toSend.CorrelationId = correlationId;
            toSend.MessageIntent = messageIntent;

            foreach (var destination in destinations)
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
                                                         ") in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file." +
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
                                            string.Join(", ", ((IBus)this).OutgoingHeaders.Select(h => h.Key + ":" + h.Value).ToArray())
                        ));

                result.Add(toSend.Id);
            }

            if (MessagesSent != null)
                MessagesSent(this, new MessagesEventArgs(messages));

            return result;
        }

        /// <summary>
        /// Takes the given message types and serializes them for inclusion in the EnclosedMessageTypes header.
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        public static string SerializeEnclosedMessageTypes(IMessage[] messages)
        {
            var types = GetFullTypes(messages);

            var sBuilder = new StringBuilder("<MessageTypes>");
            types.ForEach(s => sBuilder.Append("<s>" + s + "</s>"));
            sBuilder.Append("</MessageTypes>");

            return sBuilder.ToString();
        }

        /// <summary>
        /// Takes the serialized form of EnclosedMessageTypes and returns a list of string types.
        /// </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public static IList<string> DeserializeEnclosedMessageTypes(string serialized)
        {
            string temp = serialized.Replace("<MessageTypes><s>", "");
            temp = temp.Replace("</s></MessageTypes>", "");
            string[] arr = temp.Split(new[] { "</s><s>" }, StringSplitOptions.RemoveEmptyEntries);

            return new List<string>(arr);
        }

        private static List<string> GetFullTypes(IEnumerable<IMessage> messages)
        {
            var types = new List<string>();

            foreach (var m in messages)
            {
                var s = m.GetType().AssemblyQualifiedName;
                if (types.Contains(s))
                    continue;

                types.Add(s);

                foreach (var t in m.GetType().GetInterfaces())
                    if (typeof(IMessage).IsAssignableFrom(t) && t != typeof(IMessage))
                        if (!types.Contains(t.AssemblyQualifiedName))
                            types.Add(t.AssemblyQualifiedName);
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

                if (MessageSerializer == null)
                    throw new InvalidOperationException("No message serializer has been configured.");

                if (startupAction != null)
                    startupAction();

                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

                if (SubscriptionStorage != null)
                    SubscriptionStorage.Init();

                MessageSender.Init(Address);

                if (!string.IsNullOrEmpty(Address))
                    transport.Start();

                if (autoSubscribe)
                {
                    foreach (var messageType in GetMessageTypesHandledOnThisEndpoint())
                    {
                        var destination = GetDestinationForMessageType(messageType);
                        if (string.IsNullOrEmpty(destination))
                            continue;

                        var arr = destination.Split('@');

                        var queue = arr[0];
                        var machine = Environment.MachineName;

                        if (arr.Length == 2)
                            if (arr[1] != "." && arr[1].ToLower() != "localhost" && arr[1] != IPAddress.Loopback.ToString())
                                machine = arr[1];

                        destination = queue + "@" + machine;

                        if (destination.ToLower() != Address.ToLower())
                            Subscribe(messageType);
                    }
                }

                InitializeSelf();

                started = true;
            }

            if (Started != null)
                Started(this, null);

            return this;
        }

        private void InitializeSelf()
        {
            var toSend = GetTransportMessageFor(new CompletionMessage());
            toSend.MessageIntent = MessageIntentEnum.Init;

            MessageSender.Send(toSend, Address);
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
        /// <param name="m">The received message.</param>
        /// <remarks>
        /// run by multiple threads so must be thread safe
        /// public for testing
        /// </remarks>
        public void HandleMessage(TransportMessage m)
        {
            ForwardMessageIfNecessary(m);

            var messages = Extract(m);

            if (messages == null || messages.Length == 0)
            {
                Log.Warn("Received an empty message - ignoring.");
                return;
            }

            HandleCorellatedMessage(m, messages);

            foreach (var originalMessage in messages)
            {
                var messageToHandle = ApplyIncomingMessageMutatorsTo(originalMessage);

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
                    DispatchMessageToHandlersBasedOnType(messageToHandle, messageToHandle.GetType());
            }

            ExtensionMethods.CurrentMessageBeingHandled = null;
        }

        IMessage ApplyIncomingMessageMutatorsTo(IMessage originalMessage)
        {
            var mutators = Builder.BuildAll<IMutateIncomingMessages>();

            var mutatedMessage = originalMessage;
            mutators.ToList().ForEach(m =>
            {
                mutatedMessage = m.MutateIncoming(mutatedMessage);
            });

            return mutatedMessage;
        }

        private IMessage[] Extract(TransportMessage m)
        {
            try
            {
                return MessageSerializer.Deserialize(new MemoryStream(m.Body));
            }
            catch (Exception e)
            {
                FailureManager.SerializationFailedForMessage(m, e);
                return null;
            }
        }

        /// <summary>
        /// Finds the message handlers associated with the message type and dispatches
        /// the message to the found handlers.
        /// </summary>
        /// <param name="toHandle">The message to dispatch to the handlers.</param>
        /// <param name="messageType">The message type by which to locate the correct handlers.</param>
        /// <returns></returns>
        /// <remarks>
        /// If during the dispatch, a message handler calls the DoNotContinueDispatchingCurrentMessageToHandlers method,
        /// this prevents the message from being further dispatched.
        /// This includes generic message handlers (of IMessage), and handlers for the specific messageType.
        /// </remarks>
        private void DispatchMessageToHandlersBasedOnType(IMessage toHandle, Type messageType)
        {
            foreach (var messageHandlerType in GetHandlerTypes(messageType))
            {
                try
                {
                    Log.Debug("Activating: " + messageHandlerType.Name);

                    Builder.BuildAndDispatch(messageHandlerType, GetAction(toHandle));

                    Log.Debug(messageHandlerType.Name + " Done.");

                    if (_doNotContinueDispatchingCurrentMessageToHandlers)
                    {
                        _doNotContinueDispatchingCurrentMessageToHandlers = false;
                        return;
                    }
                }
                catch (Exception e)
                {
                    Log.Error(messageHandlerType.Name + " Failed handling message.", GetInnermostException(e));

                    throw;
                }
            }
        }

        private Action<object> GetAction<T>(T message) where T : IMessage
        {
            return (o =>
                {
                    var messageTypesToMethods = handlerToMessageTypeToHandleMethodMap[o.GetType()];
                    foreach (var messageType in messageTypesToMethods.Keys)
                        if (messageType.IsAssignableFrom(message.GetType()))
                            messageTypesToMethods[messageType].Invoke(o, new object[] { message });
                }
            );
        }

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
            var result = e;
            while (result.InnerException != null)
                result = result.InnerException;

            return result;
        }

        /// <summary>
        /// If the message contains a correlationId, attempts to
        /// invoke callbacks for that Id.
        /// </summary>
        /// <param name="msg">The message to evaluate.</param>
        /// <param name="messages">The logical messages in the transport message.</param>
        private void HandleCorellatedMessage(TransportMessage msg, IMessage[] messages)
        {
            if (msg.CorrelationId == null)
                return;

            BusAsyncResult busAsyncResult;

            lock (messageIdToAsyncResultLookup)
            {
                messageIdToAsyncResultLookup.TryGetValue(msg.CorrelationId, out busAsyncResult);
                messageIdToAsyncResultLookup.Remove(msg.CorrelationId);
            }

            if (busAsyncResult != null)
                if (messages != null)
                    if (messages.Length == 1)
                    {
                        var cm = messages[0] as CompletionMessage;
                        if (cm != null)
                            busAsyncResult.Complete(cm.ErrorCode, null);
                        else
                            busAsyncResult.Complete(int.MinValue, messages);
                    }
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
            var msg = e.Message;
            Log.Debug("Received message with ID " + msg.Id + " from sender " + msg.ReturnAddress);

            _messageBeingHandled = msg;

            if (UnitOfWorkManager != null)
                UnitOfWorkManager.Begin();

            modules = new List<IMessageModule>();
            var mods = Builder.BuildAll<IMessageModule>();
            if (mods != null)
                modules.AddRange(mods);

            foreach (var module in modules)
            {
                Log.Debug("Calling 'HandleBeginMessage' on " + module.GetType().FullName);
                module.HandleBeginMessage(); //don't need to call others if one fails
            }

            if (IsInitializationMessage(msg))
            {
                Log.Info(Address + " initialized.");
                return;
            }

            if (HandledSubscriptionMessage(msg, SubscriptionStorage, SubscriptionAuthorizer))
            {
                var messageType = GetSubscriptionMessageTypeFrom(msg);

                if (msg.MessageIntent == MessageIntentEnum.Subscribe)
                    if (ClientSubscribed != null)
                        ClientSubscribed(this, new SubscriptionEventArgs { MessageType = messageType, SubscriberAddress = msg.ReturnAddress });

                return;
            }

            _handleCurrentMessageLaterWasCalled = false;

            if (MessageReceived != null)
                MessageReceived(msg);

            if (!disableMessageHandling)
                HandleMessage(msg);

            Log.Debug("Finished handling message.");
        }

        private static string GetSubscriptionMessageTypeFrom(TransportMessage msg)
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
            string messageType = GetSubscriptionMessageTypeFrom(msg);

            Action warn = () =>
                              {
                                  var warning = string.Format("Subscription message from {0} arrived at this endpoint, yet this endpoint is not configured to be a publisher.", msg.ReturnAddress);

                                  Log.Warn(warning);

                                  if (Log.IsDebugEnabled) // only under debug, so that we don't expose ourselves to a denial of service
                                      throw new InvalidOperationException(warning);  // and cause message to go to error queue by throwing an exception
                              };

            if (msg.MessageIntent == MessageIntentEnum.Subscribe)
                if (subscriptionStorage != null)
                {
                    bool goAhead = true;
                    if (subscriptionAuthorizer != null)
                        if (!subscriptionAuthorizer.AuthorizeSubscribe(messageType, msg.ReturnAddress, msg.Headers))
                        {
                            goAhead = false;
                            Log.Debug(string.Format("Subscription request from {0} on message type {1} was refused.", msg.ReturnAddress, messageType));
                        }

                    if (goAhead)
                    {
                        Log.Info("Subscribing " + msg.ReturnAddress + " to message type " + messageType);
                        subscriptionStorage.Subscribe(msg.ReturnAddress, new[] { messageType });
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
                        if (!subscriptionAuthorizer.AuthorizeUnsubscribe(messageType, msg.ReturnAddress, msg.Headers))
                        {
                            goAhead = false;
                            Log.Debug(string.Format("Unsubscribe request from {0} on message type {1} was refused.", msg.ReturnAddress, messageType));
                        }

                    if (goAhead)
                    {
                        Log.Info("Unsubscribing " + msg.ReturnAddress + " from message type " + messageType);
                        subscriptionStorage.Unsubscribe(msg.ReturnAddress, new[] { messageType });
                    }

                    return true;
                }
                else
                {
                    warn();
                }

            return false;
        }

        private void TransportFinishedMessageProcessing(object sender, EventArgs e)
        {
            for (int i = modules.Count-1; i >= 0; i--)
            {
                Log.Debug("Calling 'HandleEndMessage' on " + modules[i].GetType().FullName);
                modules[i].HandleEndMessage();
            }

            if (UnitOfWorkManager != null)
                UnitOfWorkManager.End();
        }

        private void TransportFailedMessageProcessing(object sender, EventArgs e)
        {
            var exceptionThrown = false;

            for (int i = modules.Count - 1; i >= 0; i--)
                try
                {
                    Log.Debug("Calling 'HandleError' on " + modules[i].GetType().FullName);
                    modules[i].HandleError();
                }
                catch (Exception ex)
                {
                    Log.Error("Module " + modules[i].GetType().FullName + " failed when handling error.", ex);
                    exceptionThrown = true;
                }

            if (UnitOfWorkManager != null)
                UnitOfWorkManager.Error();

            if (exceptionThrown)
                throw new Exception("Could not handle the failed message processing correctly. Check for prior error messages in the log for more information.");
        }

        private void TransportStartedMessageProcessing(object sender, EventArgs e)
        {
        }

        private bool IsInitializationMessage(TransportMessage msg)
        {
            if (msg.ReturnAddress == null)
                return false;

            if (!msg.ReturnAddress.Contains(Address))
                return false;

            if (msg.CorrelationId != null)
                return false;

            if (msg.MessageIntent != MessageIntentEnum.Init)
                return false;

            return true;
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Sets up known types needed for XML serialization as well as
        /// to which address to send which message.
        /// </summary>
        /// <param name="owners">A dictionary of message_type, address pairs.</param>
        private void ConfigureMessageOwners(IDictionary owners)
        {
            foreach (DictionaryEntry de in owners)
            {
                var key = de.Key as string;
                if (key == null)
                    continue;

                try
                {
                    var messageType = Type.GetType(key, false);
                    if (messageType != null)
                    {
                        RegisterMessageType(messageType, de.Value as string, false);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Problem loading message type: " + key, ex);
                }

                try
                {
                    var a = Assembly.Load(key);
                    foreach (var t in a.GetTypes())
                        RegisterMessageType(t, de.Value.ToString(), true);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Problem loading message assembly: " + key, ex);
                }
            }
        }

        /// <summary>
        /// Sends the Msg to the address found in the field <see cref="ForwardReceivedMessagesTo"/>
        /// if it isn't null.
        /// </summary>
        /// <param name="m">The message to forward</param>
        private void ForwardMessageIfNecessary(TransportMessage m)
        {
            if (ForwardReceivedMessagesTo != null)
                MessageSender.Send(m, ForwardReceivedMessagesTo);
        }

        /// <summary>
        /// Registers a message type to a destination.
        /// </summary>
        /// <param name="messageType">A message type implementing <see cref="IMessage"/>.</param>
        /// <param name="destination">The address of the destination the message type is registered to.</param>
        /// <param name="configuredByAssembly">
        /// Indicates whether or not this registration call is related to a type configured from an
        /// assembly.
        /// </param>
        /// <remarks>
        /// Since the same message type may be configured specifically to one address
        /// and via its assembly to a different address, the configuredByAssembly
        /// parameter dictates that the specific message type data is to be used.
        /// </remarks>
        public void RegisterMessageType(Type messageType, string destination, bool configuredByAssembly)
        {
            if (typeof(IMessage) == messageType)
                return;

            if (typeof(IMessage).IsAssignableFrom(messageType))
            {
                if (MustNotOverrideExistingConfiguration(messageType, configuredByAssembly))
                    return;

                messageTypeToDestinationLookup[messageType] = destination;

                Log.Debug("Message " + messageType.FullName + " has been allocated to endpoint " + destination + ".");

                if (messageType.GetCustomAttributes(typeof(ExpressAttribute), true).Length == 0)
                    recoverableMessageTypes.Add(messageType);

                foreach (TimeToBeReceivedAttribute a in messageType.GetCustomAttributes(typeof(TimeToBeReceivedAttribute), true))
                    timeToBeReceivedPerMessageType[messageType] = a.TimeToBeReceived;

                return;
            }
        }

        /// <summary>
        /// Checks whether or not the existing configuration can be overridden for a message type.
        /// </summary>
        /// <param name="messageType">The type of message to check the configuration for.</param>
        /// <param name="configuredByAssembly">
        /// Indicates whether or not this check is related to a type configured from an
        /// assembly.
        /// </param>
        /// <returns>true if it is acceptable to override the configuration, otherwise false.</returns>
        private bool MustNotOverrideExistingConfiguration(Type messageType, bool configuredByAssembly)
        {
            return messageTypeToDestinationLookup.ContainsKey(messageType) && configuredByAssembly;
        }

        /// <summary>
        /// Wraps the provided messages in an NServiceBus envelope, does not include destination.
        /// </summary>
        /// <param name="rawMessages">The messages to wrap.</param>
        /// <returns>The envelope containing the messages.</returns>
        protected TransportMessage GetTransportMessageFor(params IMessage[] rawMessages)
        {
            var result = new TransportMessage
                             {
                                 Headers = new Dictionary<string, string>(), 
                                 ReturnAddress = Address
                             };

            var messages = ApplyOutgoingMessageMutatorsTo(rawMessages).ToArray();

            var ms = new MemoryStream();
            MessageSerializer.Serialize(messages, ms);
            result.Body = ms.ToArray();

            var mappers = Builder.BuildAll<IMapOutgoingTransportMessages>();
            mappers.ToList().ForEach(m => m.MapOutgoing(messages, result));

            if (PropogateReturnAddressOnSend)
                result.ReturnAddress = Address;

            var timeToBeReceived = TimeSpan.MaxValue;

            foreach (var message in messages)
            {
                if (recoverableMessageTypes.Contains(message.GetType()))
                    result.Recoverable = true;

                var span = GetTimeToBeReceivedForMessageType(message.GetType());
                timeToBeReceived = (span < timeToBeReceived ? span : timeToBeReceived);
            }

            result.TimeToBeReceived = timeToBeReceived;

            return result;
        }

        IEnumerable<IMessage> ApplyOutgoingMessageMutatorsTo(IEnumerable<IMessage> messages)
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
        /// <param name="t">The type to evaluate.</param>
        private void IfTypeIsMessageHandlerThenLoad(Type t)
        {
            if (t.IsAbstract)
                return;

            var skipHandlerRegistration = false;
            if (typeof(ISaga).IsAssignableFrom(t))
                skipHandlerRegistration = true;

            foreach (var messageType in GetMessageTypesIfIsMessageHandler(t))
            {
                if (skipHandlerRegistration)
                    continue;

                if (!handlerList.ContainsKey(t))
                    handlerList.Add(t, new List<Type>());

                if (!(handlerList[t].Contains(messageType)))
                {
                    handlerList[t].Add(messageType);
                    Log.Debug(string.Format("Associated '{0}' message with '{1}' handler", messageType, t));
                }

                if (!handlerToMessageTypeToHandleMethodMap.ContainsKey(t))
                    handlerToMessageTypeToHandleMethodMap.Add(t, new Dictionary<Type, MethodInfo>());

                MethodInfo h = GetMessageHandler(t, messageType);

                if (!(handlerToMessageTypeToHandleMethodMap[t].ContainsKey(messageType)))
                    handlerToMessageTypeToHandleMethodMap[t].Add(messageType, h);
            }
        }

        private static MethodInfo GetMessageHandler(Type targetType, Type messageType)
        {
            var method = targetType.GetMethod("Handle", new[] { messageType });
            if (method != null) return method;

            var handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);
            return targetType.GetInterfaceMap(handlerType)
            .TargetMethods
            .FirstOrDefault();
        }


        /// <summary>
        /// If the type is a message handler, returns all the message types that it handles
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static IEnumerable<Type> GetMessageTypesIfIsMessageHandler(Type type)
        {
            foreach (var t in type.GetInterfaces())
            {
                if (t.IsGenericType)
                {
                    var args = t.GetGenericArguments();
                    if (args.Length != 1)
                        continue;

                    if (!typeof(IMessage).IsAssignableFrom(args[0]))
                        continue;

                    var handlerType = typeof(IMessageHandler<>).MakeGenericType(args[0]);
                    if (handlerType.IsAssignableFrom(t))
                        yield return args[0];
                }
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
                foreach (var msgTypeHandled in handlerList[handlerType])
                    yield return msgTypeHandled;
        }

        /// <summary>
        /// Uses the first message in the array to pass to <see cref="GetDestinationForMessageType"/>.
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        protected string GetDestinationForMessages(IMessage[] messages)
        {
            if (messages == null || messages.Length == 0)
                return null;

            return GetDestinationForMessageType(messages[0].GetType());
        }

        /// <summary>
        /// Gets the destination address for a message type.
        /// </summary>
        /// <param name="messageType">The message type to get the destination for.</param>
        /// <returns>The address of the destination associated with the message type.</returns>
        protected string GetDestinationForMessageType(Type messageType)
        {
            string destination;

            lock (messageTypeToDestinationLookup)
                messageTypeToDestinationLookup.TryGetValue(messageType, out destination);

            if (destination == string.Empty)
                destination = null;

            if (destination == null)
            {
                if (messageType.IsInterface)
                    return null;

                if (messageMapper != null)
                {
                    var t = messageMapper.GetMappedTypeFor(messageType);
                    if (t != null && t != messageType)
                        return GetDestinationForMessageType(t);
                }
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

        #endregion

        #region Fields

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
        private readonly IDictionary<Type, IDictionary<Type, MethodInfo>> handlerToMessageTypeToHandleMethodMap = new Dictionary<Type, IDictionary<Type, MethodInfo>>();
        private readonly IList<Type> recoverableMessageTypes = new List<Type>();

        private readonly IDictionary<Type, TimeSpan> timeToBeReceivedPerMessageType = new Dictionary<Type, TimeSpan>();
        private readonly ReaderWriterLockSlim timeToBeReceivedPerMessageTypeLocker = new ReaderWriterLockSlim();

        /// <remarks>
        /// Accessed by multiple threads - needs appropriate locking
        /// </remarks>
        private readonly IDictionary<Type, string> messageTypeToDestinationLookup = new Dictionary<Type, string>();

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
}
