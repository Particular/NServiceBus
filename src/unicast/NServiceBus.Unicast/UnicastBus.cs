using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using Common.Logging;
using NServiceBus.Messages;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Transport;
using NServiceBus.ObjectBuilder;
using NServiceBus.MessageInterfaces;
using NServiceBus.Saga;

namespace NServiceBus.Unicast
{
	/// <summary>
	/// A unicast implementation of <see cref="IBus"/> for NServiceBus.
	/// </summary>
    public class UnicastBus : IUnicastBus, IStartableBus
    {
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

        private bool disableMessageHandling = false;

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
		/// message transport for the bus.
		/// </summary>
        public virtual ITransport Transport
        {
            set
            {
                transport = value;

                this.transport.TransportMessageReceived += transport_TransportMessageReceived;
                this.transport.FinishedMessageProcessing += transport_FinishedMessageProcessing;
            }
        }

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
        /// Should be used by programmer, not administrator.
        /// Sets an <see cref="ISubscriptionStorage"/> implementation to
		/// be used for subscription storage for the bus.
		/// </summary>
        public virtual ISubscriptionStorage SubscriptionStorage
        {
            set
            {
                this.subscriptionStorage = value;
            }
        }

        private IBuilder builder;

		/// <summary>
        /// Should be used by programmer, not administrator.
        /// Sets <see cref="IBuilder"/> implementation that will be used to 
		/// dynamically instantiate and execute message handlers.
		/// </summary>
        public virtual IBuilder Builder
        {
            set { builder = value; }
        }

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
                ExtensionMethods.MessageCreator = messageMapper;
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
        /// Should be used by programmer, not administrator.
        /// Sets whether or not the bus should impersonate the sender
		/// of a message it has received when re-sending the message.
		/// What occurs is that the thread sets its current principal
        /// to the value found in the <see cref="TransportMessage.WindowsIdentityName" />
        /// when that thread handles a message.
		/// </summary>
        public virtual bool ImpersonateSender { get; set; }

		/// <summary>
        /// Should be used by administrator, not programmer.
        /// Sets the address to which the messages received on this bus
		/// will be sent when the method HandleCurrentMessageLater is called.
		/// </summary>
        public string DistributorDataAddress { get; set; }        

        /// <summary>
        /// Should be used by administrator, not programmer.
        /// Sets the address of the distributor control queue.
        /// </summary>
        /// <remarks>
        /// Notifies the given distributor
        /// when a thread is now available to handle a new message.
        /// </remarks>
        public string DistributorControlAddress { get; set; }

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

                this.MessageHandlerTypes = types;
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

                foreach(Type t in value)
                    If_Type_Is_MessageHandler_Then_Load(t);
            }
        }
        private IEnumerable<Type> messageHandlerTypes;

        #endregion

        #region IUnicastBus Members

        /// <summary>
        /// Stops sending ready messages to the distributor, if one is configured.
        /// </summary>
        public void StopSendingReadyMessages()
        {
            this.canSendReadyMessages = false;
        }

        /// <summary>
        /// Continues sending ready messages to the distributor, if one is configured.
        /// </summary>
        public void ContinueSendingReadyMessages()
        {
            this.canSendReadyMessages = true;
        }

        /// <summary>
        /// Skips sending a ready message to the distributor once.
        /// </summary>
        public void SkipSendingReadyMessageOnce()
        {
            skipSendingReadyMessageOnce = true;
        }

        /// <summary>
        /// Event raised when no subscribers found for the published message.
        /// </summary>
        public event EventHandler<MessageEventArgs> NoSubscribersForMessage;

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
            return messageMapper.CreateInstance<T>(action);
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
            Publish(CreateInstance<T>(messageConstructor));
        }

		/// <summary>
		/// Publishes the messages to all subscribers of the first message's type.
		/// </summary>
		/// <param name="messages"></param>
        public virtual void Publish<T>(params T[] messages) where T : IMessage
        {
            if (this.subscriptionStorage == null)
                throw new InvalidOperationException("Cannot publish on this endpoint - no subscription storage has been configured. Add either 'MsmqSubscriptionStorage()' or 'DbSubscriptionStorage()' after 'NServiceBus.Configure.With()'.");

		    Type leadingType = messages[0].GetType();
            if (messageMapper != null)
                leadingType = messageMapper.GetMappedTypeFor(leadingType);

            var subscribers = this.subscriptionStorage.GetSubscribersForMessage(leadingType);
            
            if (subscribers.Count == 0)
                if (this.NoSubscribersForMessage != null)
                    this.NoSubscribersForMessage(this, new MessageEventArgs(messages[0]));

            foreach (string subscriber in subscribers)
                SendMessage(subscriber, null, messages as IMessage[]);
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
            this.Subscribe(messageType, null);
        }

        /// <summary>
        /// Subscribes to the given type T, registering a condition that all received
        /// messages of that type should comply with, otherwise discarding them.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="condition"></param>
        public void Subscribe<T>(Predicate<T> condition) where T : IMessage
        {
            Predicate<IMessage> p = new Predicate<IMessage>(m =>
            {
                if (m is T)
                    return condition((T)m);
                else
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

            this.subscriptionsManager.AddConditionForSubscriptionToMessageType(messageType, condition);

            string destination = this.GetDestinationForMessageType(messageType);

            if (destination == null)
                throw new InvalidOperationException(string.Format("No destination could be found for message type {0}. Check the <MessageEndpointMapping> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.", messageType));
            else
                this.SendMessage(destination, null, new SubscriptionMessage(messageType.AssemblyQualifiedName, SubscriptionType.Add));
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
            string destination = this.GetDestinationForMessageType(messageType);

            if (destination == null)
                throw new InvalidOperationException(string.Format("No destination could be found for message type {0}. Check the <MessageEndpointMapping> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.", messageType));
            else
                this.SendMessage(destination, null, new SubscriptionMessage(messageType.AssemblyQualifiedName, SubscriptionType.Remove));
        }

        void IBus.Reply(params IMessage[] messages)
        {
            this.SendMessage(messageBeingHandled.ReturnAddress, messageBeingHandled.IdForCorrelation, messages);
        }

        void IBus.Reply<T>(Action<T> messageConstructor)
        {
            ((IBus)this).Reply(CreateInstance<T>(messageConstructor));
        }

        void IBus.Return(int errorCode)
        {
            ((IBus)this).Reply(new CompletionMessage { ErrorCode = errorCode });
        }

        void IBus.HandleCurrentMessageLater()
        {
            if (HandleCurrentMessageLaterWasCalled)
                return;

            if (this.DistributorDataAddress != null)
                this.transport.Send(messageBeingHandled, this.DistributorDataAddress);
            else
                this.transport.ReceiveMessageLater(messageBeingHandled);

            HandleCurrentMessageLaterWasCalled = true;
        }

        /// <summary>
        /// ThreadStatic variable indicating if the current message was already
        /// marked to be handled later so we don't do this more than once.
        /// </summary>
        [ThreadStatic]
        private static bool HandleCurrentMessageLaterWasCalled;

        void IBus.SendLocal<T>(Action<T> messageConstructor)
        {
            SendLocal(CreateInstance<T>(messageConstructor));
        }

        /// <summary>
        /// Sends the list of messages back to the current bus.
        /// </summary>
        /// <param name="messages">The messages to send.</param>
        public void SendLocal(params IMessage[] messages)
        {
            //if we're a worker, send to the distributor data bus
            if (this.DistributorDataAddress != null)
            {
                TransportMessage m = this.GetTransportMessageFor(this.DistributorDataAddress, messages);

                this.transport.Send(m, this.DistributorDataAddress);
            }
            else
            {
                TransportMessage m = this.GetTransportMessageFor(this.transport.Address, messages);

                this.transport.ReceiveMessageLater(m);
            }
        }

        ICallback IBus.Send<T>(Action<T> messageConstructor)
        {
            return ((IBus)this).Send(CreateInstance<T>(messageConstructor));
        }

        ICallback IBus.Send(params IMessage[] messages)
        {
		    string destination = this.GetDestinationForMessageType(messages[0].GetType());

            return this.SendMessage(destination, null, messages);
        }

        ICallback IBus.Send<T>(string destination, Action<T> messageConstructor)
        {
            return SendMessage(destination, null, CreateInstance<T>(messageConstructor));
        }

        ICallback IBus.Send(string destination, params IMessage[] messages)
        {
            return SendMessage(destination, null, messages);
        }

        void IBus.Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            SendMessage(destination, correlationId, CreateInstance<T>(messageConstructor));
        }

        void IBus.Send(string destination, string correlationId, params IMessage[] messages)
        {
            SendMessage(destination, correlationId, messages);
        }


        private ICallback SendMessage(string destination, string correlationId, params IMessage[] messages)
        {
            AssertBusIsStarted();
            if (destination == null)
                throw new ArgumentNullException("No 'destination' specified. Messages cannot be sent.");

            TransportMessage toSend = this.GetTransportMessageFor(destination, messages);

            toSend.CorrelationId = correlationId;

            this.transport.Send(toSend, destination);

            if (log.IsDebugEnabled)
                log.Debug("Sending message " + messages[0].GetType().FullName + " to destination " + destination + ".");

            Callback result = new Callback(toSend.Id);
		    result.Registered += delegate(object sender, BusAsyncResultEventArgs args)
		                             {
                                         lock (this.messageIdToAsyncResultLookup)
                                             this.messageIdToAsyncResultLookup[args.MessageId] = args.Result;
                                     };

		    return result;
        }

		/// <summary>
		/// Starts the bus.
		/// </summary>
        public virtual IBus Start(params Action<IBuilder>[] startupActions)
        {
            if (this.started)
                return this;

            lock (this.startLocker)
            {
                if (this.started)
                    return this;

                starting = true;

                foreach (var action in startupActions)
                    action(this.builder);

                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

                if (this.subscriptionStorage != null)
                    this.subscriptionStorage.Init(this.messageTypes);

                this.transport.MessageTypesToBeReceived = this.messageTypes;
                this.transport.Start();

                if (autoSubscribe)
                {
                    foreach (Type messageType in this.GetMessageTypesHandledOnThisEndpoint())
                    {
                        string destination = this.GetDestinationForMessageType(messageType);
                        if (destination == null || destination == string.Empty)
                            continue;

                        string[] arr = destination.Split('@');

                        string queue = arr[0];
                        string machine = Environment.MachineName;

                        if (arr.Length == 2)
                            if (arr[1] != "." && arr[1].ToLower() != "localhost")
                                machine = arr[1];

                        destination = queue + "@" + machine;

                        if (destination.ToLower() != this.transport.Address.ToLower())
                            this.Subscribe(messageType);
                    }
                }

                this.InitializeSelf();

                this.SendReadyMessage(true);

                this.started = true;
            }

            return this;
        }

        private void InitializeSelf()
        {
            TransportMessage toSend = this.GetTransportMessageFor(this.transport.Address, new CompletionMessage());
            toSend.ReturnAddress = this.transport.Address; // to cancel out worker behavior

            this.transport.ReceiveMessageLater(toSend);
        }

        /// <summary>
        /// If this bus is configured to feed off of a distributor,
        /// it will send a <see cref="ReadyMessage"/> to its control address.
        /// </summary>
        /// <param name="startup"></param>
        private void SendReadyMessage(bool startup)
        {
            if (this.DistributorControlAddress == null)
                return;

            if (!this.canSendReadyMessages)
                return;

            IMessage[] messages;
            if (startup)
            {
                messages = new IMessage[this.transport.NumberOfWorkerThreads];
                for(int i=0; i < this.transport.NumberOfWorkerThreads; i++)
                {
                    ReadyMessage rm = new ReadyMessage();
                    rm.ClearPreviousFromThisAddress = (i == 0);

                    messages[i] = rm;
                }
            }
            else
            {
                messages = new IMessage[1];
                messages[0] = new ReadyMessage();
            }


            TransportMessage toSend = this.GetTransportMessageFor(this.DistributorControlAddress, messages);
            toSend.ReturnAddress = this.transport.Address;

            this.transport.Send(toSend, this.DistributorControlAddress);

            log.Debug("Sending ReadyMessage to " + this.DistributorControlAddress);
        }

        /// <summary>
        /// Tells the transport to dispose.
        /// </summary>
        public virtual void Dispose()
        {
            this.transport.Dispose();
        }


        void IBus.DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            doNotContinueDispatchingCurrentMessageToHandlers = true;
        }

        [ThreadStatic]
        private static bool doNotContinueDispatchingCurrentMessageToHandlers = false;

	    [ThreadStatic] 
        private static IDictionary<string, string> outgoingHeaders;

	    IDictionary<string, string> IBus.OutgoingHeaders
	    {
            get
            {
                if (outgoingHeaders == null)
                    outgoingHeaders = new Dictionary<string, string>();

                return outgoingHeaders;
            }
	    }

        IMessageContext IBus.CurrentMessageContext
        {
            get
            {
                return new MessageContext(messageBeingHandled);
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
            if (this.ImpersonateSender)
                Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(m.WindowsIdentityName), new string[0]);
            else
                Thread.CurrentPrincipal = null;

            ((IBus)this).OutgoingHeaders.Clear();

            this.ForwardMessageIfNecessary(m);

            this.HandleCorellatedMessage(m);

            foreach (IMessage toHandle in m.Body)
            {
                bool canDispatch = true;
                foreach (Predicate<IMessage> condition in this.subscriptionsManager.GetConditionsForMessage(toHandle))
                {
                    if (condition(toHandle) == false)
                    {
                        log.Debug(string.Format("Condition {0} failed for message {1}", condition, toHandle.GetType().Name));
                        canDispatch = false;
                        break;
                    }
                }

                if (canDispatch)
                    this.DispatchMessageToHandlersBasedOnType(toHandle, toHandle.GetType());
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
            foreach (Type messageHandlerType in this.GetHandlerTypes(messageType))
            {
                try
                {
                    log.Debug("Activating: " + messageHandlerType.Name);

                    this.builder.BuildAndDispatch(messageHandlerType, GetAction(toHandle));
                    
                    log.Debug(messageHandlerType.Name + " Done.");

                    if (doNotContinueDispatchingCurrentMessageToHandlers)
                    {
                        doNotContinueDispatchingCurrentMessageToHandlers = false;
                        return;
                    }
                }
                catch (Exception e)
                {
                    log.Error(messageHandlerType.Name + " Failed handling message.", GetInnermostException(e));

                    throw;
                }
            }

            if (toHandle is SubscriptionMessage)
            {
                if (this.subscriptionStorage != null)
                    this.subscriptionStorage.HandleSubscriptionMessage(messageBeingHandled);
                else
                {
                    string warning = string.Format("Subscription message from {0} arrived at this endpoint, yet this endpoint is not configured to be a publisher.", messageBeingHandled.ReturnAddress);

                    log.Warn(warning); // and cause message to go to error queue by throwing an exception
                    throw new InvalidOperationException(warning);
                }
            }
        }

        private Action<object> GetAction<T>(T message) where T : IMessage
        {
            return (o => 
                {
                    var messageTypesToMethods = handlerToMessageTypeToHandleMethodMap[o.GetType()];
                    foreach(Type messageType in messageTypesToMethods.Keys)
                        if (messageType.IsAssignableFrom(message.GetType()))
                            messageTypesToMethods[messageType].Invoke(o, new object[1] { message });
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
            Exception result = e;
            while (result.InnerException != null)
                result = result.InnerException;

            return result;
        }

		/// <summary>
		/// If the message contains a correlationId, attempts to
		/// invoke callbacks for that Id.
		/// </summary>
		/// <param name="msg">The message to evaluate.</param>
        private void HandleCorellatedMessage(TransportMessage msg)
        {
            if (msg.CorrelationId == null)
                return;

            BusAsyncResult busAsyncResult;

            lock (this.messageIdToAsyncResultLookup)
            {
                this.messageIdToAsyncResultLookup.TryGetValue(msg.CorrelationId, out busAsyncResult);
                this.messageIdToAsyncResultLookup.Remove(msg.CorrelationId);
            }

            if (busAsyncResult != null)
                if (msg.Body != null)
                    if (msg.Body.Length == 1)
                    {
                        CompletionMessage cm = msg.Body[0] as CompletionMessage;
                        if (cm != null)
                            busAsyncResult.Complete(cm.ErrorCode, null);
                        else
                            busAsyncResult.Complete(int.MinValue, msg.Body);
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
        private void transport_TransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            TransportMessage msg = e.Message;

            if (IsInitializationMessage(msg))
            {
                log.Info(this.transport.Address + " initialized.");
                return;
            }

            log.Debug("Received message. First element of type: " + msg.Body[0].GetType());

            messageBeingHandled = msg;
            HandleCurrentMessageLaterWasCalled = false;

            if (this.MessageReceived != null)
                this.MessageReceived(msg);

            if (!this.disableMessageHandling)
                this.HandleMessage(msg);

            log.Debug("Finished handling message.");
        }

        private void transport_FinishedMessageProcessing(object sender, EventArgs e)
        {
            if (!skipSendingReadyMessageOnce)
                this.SendReadyMessage(false);

            skipSendingReadyMessageOnce = false;
        }

        private bool IsInitializationMessage(TransportMessage msg)
        {
            if (msg.ReturnAddress == null)
                return false;

            if (!msg.ReturnAddress.Contains(this.transport.Address))
                return false;

            if (msg.CorrelationId != null)
                return false;

            if (msg.Body.Length > 1)
                return false;

            // A CompletionMessage is used out of convenience as the initialization message.
            CompletionMessage em = msg.Body[0] as CompletionMessage;
            if (em == null)
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
                try
                {
                    Type messageType = Type.GetType(de.Key as string, false);
                    if (messageType != null)
                    {
                        this.RegisterMessageType(messageType, de.Value as string, false);
                        continue;
                    }
                }
                catch (Exception)
                {
                }

                try
                {
                    Assembly a = Assembly.Load(de.Key.ToString());
                    foreach (Type t in a.GetTypes())
                        this.RegisterMessageType(t, de.Value.ToString(), true);
                }
                catch (Exception)
                {
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
            if (this.ForwardReceivedMessagesTo != null)
                this.transport.Send(m, this.ForwardReceivedMessagesTo);
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
                if (this.MustNotOverrideExistingConfiguration(messageType, configuredByAssembly))
                    return;

                this.messageTypeToDestinationLookup[messageType] = destination;
                this.AddMessageType(messageType);

                if (messageType.GetCustomAttributes(typeof(RecoverableAttribute), true).Length > 0)
                    recoverableMessageTypes.Add(messageType);

                foreach (TimeToBeReceivedAttribute a in messageType.GetCustomAttributes(typeof(TimeToBeReceivedAttribute), true))
                    timeToBeReceivedPerMessageType[messageType] = a.TimeToBeReceived;

                return;
            }            
        }

	    /// <summary>
        /// Should be used by programmer, not administrator.
        /// </summary>
        /// <param name="messageType"></param>
        public void AddMessageType(Type messageType)
        {
            if (!this.messageTypes.Contains(messageType))
            {
                this.messageTypes.Add(messageType);
                log.Debug("Registered message " + messageType.FullName);
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
            return this.messageTypeToDestinationLookup.ContainsKey(messageType) && configuredByAssembly;
        }

		/// <summary>
		/// Wraps the provided messages in an NServiceBus envelope.
		/// </summary>
        /// <param name="destination">The destination to which to send the messages.</param>
        /// <param name="messages">The messages to wrap.</param>
        /// <returns>The envelope containing the messages.</returns>
        protected TransportMessage GetTransportMessageFor(string destination, params IMessage[] messages)
        {
            TransportMessage result = new TransportMessage();
            result.Body = messages;

            result.ReturnAddress = this.GetReturnAddressFor(destination);

            result.WindowsIdentityName = Thread.CurrentPrincipal.Identity.Name;

            if (this.PropogateReturnAddressOnSend)
                result.ReturnAddress = this.transport.Address;

		    result.Headers = HeaderAdapter.From(outgoingHeaders);

            TimeSpan timeToBeReceived = TimeSpan.MaxValue;

            foreach (IMessage message in messages)
            {
                if (recoverableMessageTypes.Contains(message.GetType()))
                    result.Recoverable = true;

                if (timeToBeReceivedPerMessageType.ContainsKey(message.GetType()))
                {
                    TimeSpan span = timeToBeReceivedPerMessageType[message.GetType()];
                    timeToBeReceived = (span < timeToBeReceived ? span : timeToBeReceived);
                }
            }

            result.TimeToBeReceived = timeToBeReceived;

            return result;
        }

        private string GetReturnAddressFor(string destination)
        {
            string result = this.transport.Address;

            // if we're a worker
            if (this.DistributorDataAddress != null)
            {
                result = this.DistributorDataAddress;

                //if we're sending a message to the control bus, then use our own address
                if (destination == this.DistributorControlAddress)
                    result = this.transport.Address;
            }

            return result;
        }

		/// <summary>
		/// Evaluates a type and loads it if it implements IMessageHander{T}.
		/// </summary>
		/// <param name="t">The type to evaluate.</param>
        private void If_Type_Is_MessageHandler_Then_Load(Type t)
        {
            if (t.IsAbstract)
                return;

            bool skipHandlerRegistration = false;
            if (typeof(ISaga).IsAssignableFrom(t))
                skipHandlerRegistration = true;

            foreach(Type messageType in GetMessageTypesIfIsMessageHandler(t))
            {
                foreach (Type msgType in messageType.Assembly.GetTypes())
                    if (typeof(IMessage).IsAssignableFrom(msgType))
                        AddMessageType(msgType);

                if (skipHandlerRegistration)
                    continue;

                if (!handlerList.ContainsKey(t))
                    handlerList.Add(t, new List<Type>());

                if (!(handlerList[t].Contains(messageType)))
                {
                    handlerList[t].Add(messageType);
                    log.Debug(string.Format("Associated '{0}' message with '{1}' handler", messageType, t));
                }

                if (!handlerToMessageTypeToHandleMethodMap.ContainsKey(t))
                    handlerToMessageTypeToHandleMethodMap.Add(t, new Dictionary<Type, MethodInfo>());

                if (!(handlerToMessageTypeToHandleMethodMap[t].ContainsKey(messageType)))
                    handlerToMessageTypeToHandleMethodMap[t].Add(messageType, t.GetMethod("Handle", new Type[1] { messageType }));
            }
        }

        /// <summary>
        /// If the type is a message handler, returns all the message types that it handles
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private IEnumerable<Type> GetMessageTypesIfIsMessageHandler(Type type)
        {
            foreach (Type t in type.GetInterfaces())
            {
                if (t.IsGenericType)
                {
                    Type[] args = t.GetGenericArguments();
                    if (args.Length != 1)
                        continue;

                    if (!typeof (IMessage).IsAssignableFrom(args[0]))
                        continue;

                    Type handlerType = typeof (IMessageHandler<>).MakeGenericType(args[0]);
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
            foreach (Type handlerType in this.handlerList.Keys)
                foreach (Type msgTypeHandled in this.handlerList[handlerType])
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
            foreach (Type handlerType in this.handlerList.Keys)
                foreach (Type msgTypeHandled in this.handlerList[handlerType])
                    yield return msgTypeHandled;
       }

		/// <summary>
		/// Gets the destination address for a message type.
		/// </summary>
		/// <param name="messageType">The message type to get the destination for.</param>
		/// <returns>The address of the destination associated with the message type.</returns>
        protected string GetDestinationForMessageType(Type messageType)
        {
            string destination;

            lock (this.messageTypeToDestinationLookup)
                this.messageTypeToDestinationLookup.TryGetValue(messageType, out destination);

            if (destination == string.Empty)
                destination = null;

            if (destination == null)
            {
                if (messageType.IsInterface)
                    return null;

                if (messageMapper != null)
                {
                    Type t = messageMapper.GetMappedTypeFor(messageType);
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
            if(starting == false)
                throw new InvalidOperationException("The bus is not started yet, call Bus.Start() before attempting to use the bus.");
        }

        #endregion

        #region Fields

        /// <summary>
		/// Gets/sets the subscription manager to use for the bus.
		/// </summary>
        protected SubscriptionsManager subscriptionsManager = new SubscriptionsManager();

        /// <summary>
        /// Gets/sets the subscription storage.
        /// </summary>
	    protected ISubscriptionStorage subscriptionStorage;

        private readonly List<Type> messageTypes = new List<Type>(new Type[] { typeof(CompletionMessage), typeof(SubscriptionMessage), typeof(ReadyMessage), typeof(IMessage[]) });

        private readonly IDictionary<Type, List<Type>> handlerList = new Dictionary<Type, List<Type>>();
        private readonly IDictionary<Type, IDictionary<Type, MethodInfo>> handlerToMessageTypeToHandleMethodMap = new Dictionary<Type, IDictionary<Type, MethodInfo>>();
        private readonly IDictionary<string, BusAsyncResult> messageIdToAsyncResultLookup = new Dictionary<string, BusAsyncResult>();
	    private readonly IList<Type> recoverableMessageTypes = new List<Type>();
	    private readonly IDictionary<Type, TimeSpan> timeToBeReceivedPerMessageType = new Dictionary<Type, TimeSpan>();

        /// <remarks>
        /// Accessed by multiple threads - needs appropriate locking
		/// </remarks>
        private readonly IDictionary<Type, string> messageTypeToDestinationLookup = new Dictionary<Type, string>();

		/// <remarks>
        /// ThreadStatic
		/// </remarks>
        [ThreadStatic]
        static TransportMessage messageBeingHandled;

        /// <summary>
        /// Accessed by multiple threads.
        /// </summary>
        private volatile bool canSendReadyMessages = true;

        /// <summary>
        /// ThreadStatic
        /// </summary>
	    [ThreadStatic] private static bool skipSendingReadyMessageOnce;

	    private volatile bool started = false;
        private volatile bool starting = false;
        private readonly object startLocker = new object();

        private readonly static ILog log = LogManager.GetLogger(typeof(UnicastBus));
        #endregion
    }
}
