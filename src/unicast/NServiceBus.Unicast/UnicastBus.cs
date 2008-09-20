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
using ObjectBuilder;
using NServiceBus.MessageInterfaces;

namespace NServiceBus.Unicast
{
	/// <summary>
	/// A unicast implementation of <see cref="IBus"/> for NServiceBus.
	/// </summary>
    public class UnicastBus : IUnicastBus
    {
        #region config properties

        private bool disableMessageHandling = false;

		/// <summary>
        /// Should be used by programmer, not administrator.
        /// Disables the handling of incoming messages.
		/// </summary>
        public virtual bool DisableMessageHandling
        {
            set { disableMessageHandling = value; }
        }

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

	    public virtual IMessageMapper MessageMapper
	    {
            get { return messageMapper; }
            set { messageMapper = value; }
	    }

        private bool propogateReturnAddressOnSend = false;

		/// <summary>
        /// Should be used by programmer, not administrator.
        /// Sets whether or not the return address of a received message 
		/// should be propogated when the message is forwarded. This field is
		/// used primarily for the Distributor.
		/// </summary>
        public virtual bool PropogateReturnAddressOnSend
        {
            set { propogateReturnAddressOnSend = value; }
        }

        private bool impersonateSender;

		/// <summary>
        /// Should be used by programmer, not administrator.
        /// Sets whether or not the bus should impersonate the sender
		/// of a message it has received when re-sending the message.
		/// What occurs is that the thread sets its current principal
        /// to the value found in the <see cref="TransportMessage.WindowsIdentityName" />
        /// when that thread handles a message.
		/// </summary>
        public virtual bool ImpersonateSender
        {
            set { impersonateSender = value; }
        }

        private string distributorDataAddress;

		/// <summary>
        /// Should be used by administrator, not programmer.
        /// Sets the address to which the messages received on this bus
		/// will be sent when the method HandleCurrentMessageLater is called.
		/// </summary>
        public virtual string DistributorDataAddress
        {
            set { distributorDataAddress = value; }
        }        
        
        private string distributorControlAddress;

        /// <summary>
        /// Should be used by administrator, not programmer.
        /// Sets the address of the distributor control queue.
        /// </summary>
        /// <remarks>
        /// Notifies the given distributor
        /// when a thread is now available to handle a new message.
        /// </remarks>
        public virtual string DistributorControlAddress
        {
            set { distributorControlAddress = value; }
        }


	    private string forwardReceivedMessagesTo;

        /// <summary>
        /// Should be used by administrator, not programmer.
        /// Sets the address to which all messages received on this bus will be 
        /// forwarded to (not including subscription messages). 
        /// This is primarily useful for smart client scenarios 
        /// where both client and server software are installed on the mobile
        /// device. The server software will have this field set to the address
        /// of the real server.
        /// </summary>
	    public virtual string ForwardReceivedMessagesTo
	    {
	        set { forwardReceivedMessagesTo = value; }
	    }

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
        public virtual IDictionary MessageOwners
        {
            set
            {
                ConfigureMessageOwners(value);
            }
        }

        /// <summary>
        /// Sets the list of assemblies which contain a message handlers
		/// for the bus.
        /// </summary>
        public virtual IList MessageHandlerAssemblies
        {
            set
            {
                foreach (Assembly a in value)
                {
                    try
                    {
                        this.AddTypesFromAssembly(a);
                    }
                    catch(Exception e)
                    {
                        log.Error("Problems analyzing " + a.FullName, e);
                    }
                }
            }
        }

        #endregion

        #region IUnicastBus Members

        public void StopSendingReadyMessages()
        {
            this.canSendReadyMessages = false;
        }

        public void ContinueSendingReadyMessages()
        {
            this.canSendReadyMessages = true;
        }

        #endregion

        #region IBus Members

		/// <summary>
		/// Publishes the first message in the list to all subscribers of that message type.
		/// </summary>
		/// <param name="messages">A list of messages.  Only the first will be published.</param>
        public virtual void Publish<T>(params T[] messages) where T : IMessage
        {
		    Type leadingType = messages[0].GetType();
            if (messageMapper != null)
                leadingType = messageMapper.GetMappedTypeFor(leadingType);

            foreach (string subscriber in this.subscriptionStorage.GetSubscribersForMessage(leadingType))
                try
                {
                    this.Send(subscriber, messages as IMessage[]);
                }
                catch(Exception e)
                {
                    log.Error("Problem sending message to subscriber: " + subscriber, e);
                }
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
		/// Subscribes to receive published messages of the specified type if
		/// they meet the provided condition.
		/// </summary>
		/// <param name="messageType">The type of message to subscribe to.</param>
		/// <param name="condition">The condition under which to receive the message.</param>
        public virtual void Subscribe(Type messageType, Predicate<IMessage> condition)
        {
            this.subscriptionsManager.AddConditionForSubscriptionToMessageType(messageType, condition);

            string destination = this.GetDestinationForMessageType(messageType);

            this.Send(destination, new SubscriptionMessage(messageType.AssemblyQualifiedName, SubscriptionType.Add));
        }

		/// <summary>
		/// Unsubscribes from receiving published messages of the specified type.
		/// </summary>
		/// <param name="messageType"></param>
        public virtual void Unsubscribe(Type messageType)
        {
            string destination = this.GetDestinationForMessageType(messageType);

            this.Send(destination, new SubscriptionMessage(messageType.AssemblyQualifiedName, SubscriptionType.Remove));
        }

		/// <summary>
		/// Sends all messages to the destination found in <see cref="SourceOfMessageBeingHandled"/>.
		/// </summary>
		/// <param name="messages">The messages to send.</param>
        public void Reply(params IMessage[] messages)
        {
            TransportMessage toSend = this.GetTransportMessageFor(messageBeingHandled.ReturnAddress, messages);

            toSend.CorrelationId = messageBeingHandled.IdForCorrelation;

            this.transport.Send(toSend, messageBeingHandled.ReturnAddress);
        }

		/// <summary>
		/// Returns an completion message with the specified error code to the sender
		/// of the message being handled.
		/// </summary>
		/// <param name="errorCode">An code specifying the result.</param>
        public void Return(int errorCode)
        {
            CompletionMessage msg = new CompletionMessage();
            msg.ErrorCode = errorCode;

            this.Reply(msg);
        }

		/// <summary>
		/// Causes the message being handled to be moved to the back of the list of available 
		/// messages so it can be handled later.
		/// </summary>
        public void HandleCurrentMessageLater()
        {
            if (this.distributorDataAddress != null)
                this.transport.Send(messageBeingHandled, this.distributorDataAddress);
            else
                this.transport.ReceiveMessageLater(messageBeingHandled);
        }

        /// <summary>
        /// Sends the list of messages back to the current bus.
        /// </summary>
        /// <param name="messages">The messages to send.</param>
        public void SendLocal(params IMessage[] messages)
        {
            //if we're a worker, send to the distributor data bus
            if (this.distributorDataAddress != null)
            {
                TransportMessage m = this.GetTransportMessageFor(this.distributorDataAddress, messages);

                this.transport.Send(m, this.distributorDataAddress);
            }
            else
            {
                TransportMessage m = this.GetTransportMessageFor(this.transport.Address, messages);

                this.transport.ReceiveMessageLater(m);
            }
        }

		/// <summary>
		/// Sends the list of provided messages and calls the provided <see cref="AsyncCallback"/> delegate
		/// when the message is completed.
		/// </summary>
        /// <param name="messages">The list of messages to send.</param>
        /// <remarks>
		/// All the messages will be sent to the destination configured for the
		/// first message in the list.
		/// </remarks>
        public ICallback Send(params IMessage[] messages)
        {
		    string destination = this.GetDestinationForMessageType(messages[0].GetType());

            return this.Send(destination, messages);
        }

		/// <summary>
		/// Sends the list of provided messages and calls the provided <see cref="AsyncCallback"/> delegate
		/// when the message is completed.
		/// </summary>
		/// <param name="destination">The address of the destination to send the messages to.</param>
        /// <param name="messages">The list of messages to send.</param>
        public ICallback Send(string destination, params IMessage[] messages)
        {
            TransportMessage toSend = this.GetTransportMessageFor(destination, messages);
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
		/// Gets the address from which the message being handled was sent.
		/// </summary>
        public string SourceOfMessageBeingHandled
        {
            get
            {
                if (messageBeingHandled != null)
                    return messageBeingHandled.ReturnAddress;

                return null;
            }
        }

		/// <summary>
		/// Starts the bus.
		/// </summary>
        public virtual void Start()
        {
            if (this.started)
                return;

            lock (this.startLocker)
            {
                if (this.started)
                    return;

                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

                if (this.subscriptionStorage != null)
                    this.subscriptionStorage.Init();

                this.transport.MessageTypesToBeReceived = this.messageTypes;

                this.transport.Start();

                this.InitializeSelf();

                for (int i = 0; i < this.transport.NumberOfWorkerThreads; i++)
                    this.SendReadyMessage(i == 0);

                this.started = true;
            }
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
            if (this.distributorControlAddress == null)
                return;

            if (!this.canSendReadyMessages)
                return;

            ReadyMessage rm = new ReadyMessage();
            rm.NumberOfWorkerThreads = this.transport.NumberOfWorkerThreads;

            if (startup)
                rm.ClearPreviousFromThisAddress = true;

            TransportMessage toSend = this.GetTransportMessageFor(this.distributorControlAddress, rm);
            toSend.ReturnAddress = this.transport.Address;

            this.transport.Send(toSend, this.distributorControlAddress);

            log.Debug("Sending ReadyMessage to " + this.distributorControlAddress);
        }

        public virtual void Dispose()
        {
            this.transport.Dispose();
        }

		/// <summary>
		/// Tells the bus to stop dispatching the current message to additional
		/// handlers.
		/// </summary>
        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            doNotContinueDispatchingCurrentMessageToHandlers = true;
        }

        [ThreadStatic]
        private static bool doNotContinueDispatchingCurrentMessageToHandlers = false;

	    [ThreadStatic] 
        private static IDictionary<string, string> outgoingHeaders;

	    public IDictionary<string, string> OutgoingHeaders
	    {
            get
            {
                if (outgoingHeaders == null)
                    outgoingHeaders = new Dictionary<string, string>();

                return outgoingHeaders;
            }
	    }

	    public IDictionary<string, string> IncomingHeaders
	    {
            get
            {
                return new HeaderAdapter(messageBeingHandled.Headers);
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
            if (this.impersonateSender)
                Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(m.WindowsIdentityName), new string[0]);
            else
                Thread.CurrentPrincipal = null;

            OutgoingHeaders.Clear();

            this.ForwardMessageIfNecessary(m);

            if (IsSubscriptionMessage(m))
                if (this.subscriptionStorage.HandledSubscriptionMessage(m))
                    return;

            this.HandleCorellatedMessage(m);

            foreach (IMessage toHandle in m.Body)
            {
                foreach (Predicate<IMessage> condition in this.subscriptionsManager.GetConditionsForMessage(toHandle))
                {
                    if (condition(toHandle) == false)
                    {
                        log.Debug(string.Format("Condition {0} failed for message {1}", condition, toHandle.GetType().Name));
                        return;
                    }
                }

                this.DispatchMessageToHandlersBasedOnType(toHandle, toHandle.GetType());
            }
        }

        private static bool IsSubscriptionMessage(TransportMessage m)
        {
            IMessage[] messages = m.Body;
            if (messages == null)
                return false;

            if (messages.Length != 1)
                return false;

            SubscriptionMessage subMessage = messages[0] as SubscriptionMessage;
            if (subMessage != null)
                return true;

            return false;
        }

		/// <summary>
		/// Finds the message handlers associated with the message type and dispatches
		/// the message to the found handlers.
		/// </summary>
		/// <param name="toHandle">The message to dispatch to the handlers.</param>
		/// <param name="messageType">The message type by which to locate the correct handlers.</param>
		/// <returns></returns>
		/// <remarks>
		/// If during the dispatch, a message handler calls the <see cref="DoNotContinueDispatchingCurrentMessageToHandlers"/> method,
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

                    this.builder.BuildAndDispatch(messageHandlerType, "Handle", toHandle);
                    
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

            return;
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

            if (this.MessageReceived != null)
                this.MessageReceived(msg);

            if (!this.disableMessageHandling)
                this.HandleMessage(msg);

            this.SendReadyMessage(false);

            log.Debug("Finished handling message.");
        }

		/// <summary>
		/// Checks whether a received message is an initialization message.
		/// </summary>
		/// <param name="msg">The message to check.</param>
		/// <returns>true if the message is an initialization message, otherwise false.</returns>
		/// <remarks>
		/// A <see cref="CompletionMessage"/> is used out of convenience as the initialization message.</remarks>
        private bool IsInitializationMessage(TransportMessage msg)
        {
            if (!msg.ReturnAddress.Contains(this.transport.Address))
                return false;

            if (msg.CorrelationId != null)
                return false;

            if (msg.Body.Length > 1)
                return false;

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
                Type messageType = Type.GetType(de.Key.ToString(), false);
                if (messageType != null)
                {
                    this.RegisterMessageTypeToDestination(messageType, de.Value.ToString(), false);
                    continue;
                }

                Assembly a = Assembly.Load(de.Key.ToString());
                foreach (Type t in a.GetTypes())
                    this.RegisterMessageTypeToDestination(t, de.Value.ToString(), true);
            }
        }

        /// <summary>
        /// Sends the Msg to the address found in the field <see cref="forwardReceivedMessagesTo"/>
        /// if it isn't null.
        /// </summary>
        /// <param name="m">The message to forward</param>
        private void ForwardMessageIfNecessary(TransportMessage m)
        {
            if (this.forwardReceivedMessagesTo != null)
                this.transport.Send(m, this.forwardReceivedMessagesTo);
        }

		/// <summary>
		/// Adds types from an assembly to the list of registered message types and handlers 
		/// for the bus.
		/// </summary>
		/// <param name="a">The assembly to process.</param>
		/// <remarks>
		/// If a type implements <see cref="IMessage"/> it will be added to the list
		/// of message types registered to the bus.  If a type implements IMessageHandler
		/// it will be added to the list of message handlers for the bus.</remarks>
        public void AddTypesFromAssembly(Assembly a)
        {
            foreach (Type t in a.GetTypes())
            {
                if (typeof(IMessage).IsAssignableFrom(t) && !t.IsAbstract)
                {
                    this.messageTypes.Add(t);
                    if(log.IsDebugEnabled)
                        log.Debug(string.Format("Registered message '{0}'", t));
                    continue;
                }

                If_Type_Is_MessageHandler_Then_Load(t);
            }
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
		/// and via its assembly to a different address, the <see cref="configuredByAssembly"/>
		/// parameter dictates that the specific message type data is to be used.
		/// </remarks>
        public void RegisterMessageTypeToDestination(Type messageType, string destination, bool configuredByAssembly)
        {
            if (typeof(IMessage) == messageType)
                return;

            if (typeof(IMessage).IsAssignableFrom(messageType))
            {
                if (this.MustNotOverrideExistingConfiguration(messageType, configuredByAssembly))
                    return;

                this.messageTypeToDestinationLookup[messageType] = destination;
                this.AddMessageType(messageType);

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
                this.messageTypes.Add(messageType);
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

            if (this.propogateReturnAddressOnSend)
                result.ReturnAddress = this.transport.Address;

		    result.Headers = HeaderAdapter.From(OutgoingHeaders);

            TimeSpan timeToBeReceived = TimeSpan.MaxValue;

            foreach (IMessage message in messages)
            {
                if (message.GetType().GetCustomAttributes(typeof(RecoverableAttribute), true).Length > 0)
                    result.Recoverable = true;

                foreach (TimeToBeReceivedAttribute a in message.GetType().GetCustomAttributes(typeof(TimeToBeReceivedAttribute), true))
                    timeToBeReceived = (a.TimeToBeReceived < timeToBeReceived ? a.TimeToBeReceived : timeToBeReceived);
            }

            result.TimeToBeReceived = timeToBeReceived;

            return result;
        }

        private string GetReturnAddressFor(string destination)
        {
            string result = this.transport.Address;

            // if we're a worker
            if (this.distributorDataAddress != null)
            {
                result = this.distributorDataAddress;

                //if we're sending a message to the control bus, then use our own address
                if (destination == this.distributorControlAddress)
                    result = this.transport.Address;
            }

            return result;
        }

		/// <summary>
		/// Evaluates a type and loads it if it implements IMessageHander<T>.
		/// </summary>
		/// <param name="t">The type to evaluate.</param>
        private void If_Type_Is_MessageHandler_Then_Load(Type t)
        {
            if (t.IsAbstract)
                return;

            foreach(Type messageType in GetMessageTypesIfIsMessageHandler(t))
            {
                if (!handlerList.ContainsKey(t))
                    handlerList.Add(t, new List<Type>());

                if (!(handlerList[t].Contains(messageType)))
                {
                    handlerList[t].Add(messageType);
                    log.Debug(string.Format("Associated '{0}' message with '{1}' handler", messageType, t));
                }
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
		/// Gets the destination address for a message type.
		/// </summary>
		/// <param name="messageType">The message type to get the destination for.</param>
		/// <returns>The address of the destination associated with the message type.</returns>
        protected string GetDestinationForMessageType(Type messageType)
        {
            string destination;

            lock (this.messageTypeToDestinationLookup)
                this.messageTypeToDestinationLookup.TryGetValue(messageType, out destination);

            if (destination == null)
            {
                if (messageMapper != null)
                {
                    Type t = messageMapper.GetMappedTypeFor(messageType);
                    if (t != null)
                        return GetDestinationForMessageType(t);
                }

                throw new ConfigurationException(
                    string.Format("No destination could be found for message type {0}.", messageType)
                    );
            }

		    return destination;
        }

        #endregion

        #region Fields

		/// <summary>
		/// Gets/sets the subscription manager to use for the bus.
		/// </summary>
        protected SubscriptionsManager subscriptionsManager = new SubscriptionsManager();

	    protected ISubscriptionStorage subscriptionStorage;

        private readonly List<Type> messageTypes = new List<Type>(new Type[] { typeof(CompletionMessage), typeof(SubscriptionMessage), typeof(ReadyMessage), typeof(IMessage[]) });

        private readonly IDictionary<Type, List<Type>> handlerList = new Dictionary<Type, List<Type>>();
        private readonly IDictionary<string, BusAsyncResult> messageIdToAsyncResultLookup = new Dictionary<string, BusAsyncResult>();

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

	    private volatile bool started = false;
        private object startLocker = new object();

        private readonly static ILog log = LogManager.GetLogger(typeof(UnicastBus));
        #endregion
    }
}
