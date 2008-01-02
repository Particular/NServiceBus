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

namespace NServiceBus.Unicast
{
	/// <summary>
	/// A unicast implementation of <see cref="IBus"/> for NServiceBus.
	/// </summary>
    public class UnicastBus : IBus
    {
        #region config properties

        private bool disableMessageHandling = false;

		/// <summary>
		/// Disables the handling of incoming messages.
		/// </summary>
        public bool DisableMessageHandling
        {
            set { disableMessageHandling = value; }
        }

        protected ITransport transport;

		/// <summary>
		/// Sets an <see cref="ITransport"/> implementation to use as the
		/// message transport for the bus.
		/// </summary>
        public ITransport Transport
        {
            set
            {
                transport = value;

                this.transport.MsgReceived += transport_MsgReceived;
            }
        }

		/// <summary>
		/// A delegate for a method that will handle the <see cref="MessageReceived"/>
		/// event.
		/// </summary>
		/// <param name="message">The message received.</param>
        public delegate void MessageReceivedDelegate(Msg message);

		/// <summary>
		/// Event raised when a message is received.
		/// </summary>
        public event MessageReceivedDelegate MessageReceived;

		/// <summary>
		/// Sets an <see cref="ISubscriptionStorage"/> implementation to
		/// be used for subscription storage for the bus.
		/// </summary>
        public ISubscriptionStorage SubscriptionStorage
        {
            set
            {
                this.subscriptionsManager.Storage = value;
            }
        }

        private IBuilder builder;

		/// <summary>
		/// Sets <see cref="IBuilder"/> implementation that will be used to 
		/// dynamically instantiate and execute message handlers.
		/// </summary>
        public IBuilder Builder
        {
            set { builder = value; }
        }

        private bool propogateReturnAddressOnSend = false;

		/// <summary>
		/// Sets whether or not the return address of a received message 
		/// should be propogated when the message is forwarded. This field is
		/// used primarily for the Distributor.
		/// </summary>
        public bool PropogateReturnAddressOnSend
        {
            set { propogateReturnAddressOnSend = value; }
        }

        private bool impersonateSender;

		/// <summary>
		/// Sets whether or not the bus should impersonate the sender
		/// of a message it has received when re-sending the message.
		/// What occurs is that the thread sets its current principal
        /// to the value found in the <see cref="Msg.WindowsIdentityName" />
        /// when that thread handles a message.
		/// </summary>
        public bool ImpersonateSender
        {
            set { impersonateSender = value; }
        }

        private string distributorDataAddress;

		/// <summary>
		/// Sets the address to which the messages received on this bus
		/// will be sent when the method HandleCurrentMessageLater is called.
		/// </summary>
        public string DistributorDataAddress
        {
            set { distributorDataAddress = value; }
        }

	    private string forwardReceivedMessagesTo;

        /// <summary>
        /// Sets the address to which all messages received on this bus will be 
        /// forwarded to (not including subscription messages). 
        /// This is primarily useful for smart client scenarios 
        /// where both client and server software are installed on the mobile
        /// device. The server software will have this field set to the address
        /// of the real server.
        /// </summary>
	    public string ForwardReceivedMessagesTo
	    {
	        set { forwardReceivedMessagesTo = value; }
	    }

		/// <summary>
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
            set
            {
                ConfigureMessageOwners(value);
            }
        }

        /// <summary>
        /// Sets the list of assembly names which contain a message handlers
		/// for the bus.
        /// </summary>
        public IList MessageHandlerAssemblies
        {
            set
            {
                foreach (string s in value)
                {
                    try
                    {
                        Assembly a = Assembly.Load(s);
                        this.AddTypesFromAssembly(a);
                    }
                    catch(Exception e)
                    {
                        log.Error("Problems analyzing " + s, e);
                    }
                }
            }
        }

        #endregion

        #region IBus Members

		/// <summary>
		/// Publishes the first message in the list to all subscribers of that message type.
		/// </summary>
		/// <param name="messages">A list of messages.  Only the first will be published.</param>
        public virtual void Publish<T>(params T[] messages) where T : IMessage
        {
            foreach (string subscriber in this.subscriptionsManager.GetSubscribersForMessage(messages[0]))
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
            Msg toSend = this.GetMsgFor(messages);

            toSend.CorrelationId = messageBeingHandled.Id;

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
            HandleMsgLater(messageBeingHandled);
        }

		/// <summary>
		/// Moves the specified messages to the back of the list of available 
		/// messages so they can be handled later.
		/// </summary>
		/// <param name="messages">The messages to handle later.</param>
        public void HandleMessagesLater(params IMessage[] messages)
        {
            Msg m = this.GetMsgFor(messages);

            this.HandleMsgLater(m);
        }

        /// <summary>
        /// Sends the list of messages back to the current bus.
        /// </summary>
        /// <param name="messages">The messages to send.</param>
        public void SendLocal(params IMessage[] messages)
        {
            Msg m = this.GetMsgFor(messages);

            this.transport.ReceiveMessageLater(m);
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
            string destination = this.messageTypeToDestinationLookup[messages[0].GetType()];

            return this.Send(destination, messages);
        }

		/// <summary>
		/// Sends the list of provided messages and calls the provided <see cref="AsyncCallback"/> delegate
		/// when the message is completed.
		/// </summary>
		/// <param name="destination">The address of the destination to send the messages to.</param>
        /// <param name="messages">The list of messages to send.</param>
        /// <remarks>
		/// All the messages will be sent to the destination configured for the
		/// first message in the list.
		/// </remarks>
        public ICallback Send(string destination, params IMessage[] messages)
        {
            Msg toSend = this.GetMsgFor(messages);
            this.transport.Send(toSend, destination);

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
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            this.transport.MessageTypesToBeReceived = this.messageTypes;

            this.transport.Start();

            this.SendLocal(new CompletionMessage());
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
		public void HandleMessage(Msg m)
        {
            if (this.subscriptionsManager.HandledSubscriptionMessage(m))
                return;

            this.ForwardMessageIfNecessary(m);

            if (this.HandledCompletionMessage(m))
                return;

            if (this.impersonateSender)
                Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(m.WindowsIdentityName), new string[0]);

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

                if (this.DispatchMessageToHandlersBasedOnType(toHandle, typeof(IMessage)))
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
		/// If during the dispatch, a message handler calls the <see cref="DoNotContinueDispatchingCurrentMessageToHandlers"/> method,
		/// this prevents the message from being further dispatched.
		/// This includes generic message handlers (of IMessage), and handlers for the specific messageType.
		/// </remarks>
        private bool DispatchMessageToHandlersBasedOnType(IMessage toHandle, Type messageType)
        {
            foreach (Type messageHandlerType in this.GetHandlerTypes(messageType))
            {
                try
                {
                    log.Debug("Activating: " + messageHandlerType.Name);

                    this.builder.BuildAndDispatch(messageHandlerType, "Handle", toHandle);
                    
                    log.Debug(messageHandlerType.Name + " Done.");

                    if (doNotContinueDispatchingCurrentMessageToHandlers)
                        return false;
                }
                catch (Exception e)
                {
                    log.Error(messageHandlerType.Name + " Failed handling message.", GetInnermostException(e));

                    throw;
                }
            }

            return true;
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
		/// Returns whether or not the message is a completion message.
		/// </summary>
		/// <param name="msg">The message to evaluate.</param>
		/// <returns>true if the message is an ErrorMessage, otherwise false.</returns>
        private bool HandledCompletionMessage(Msg msg)
        {
            if (msg.Body.Length != 1)
                return false;

            CompletionMessage errorMessage = msg.Body[0] as CompletionMessage;
            if (errorMessage != null)
            {
                if (msg.CorrelationId == null)
                    return true;

                BusAsyncResult busAsyncResult;

                lock (this.messageIdToAsyncResultLookup)
                {
                    this.messageIdToAsyncResultLookup.TryGetValue(msg.CorrelationId, out busAsyncResult);
                    this.messageIdToAsyncResultLookup.Remove(msg.CorrelationId);
                }

                if (busAsyncResult != null)
                    busAsyncResult.Complete(errorMessage.ErrorCode);

                return true;
            }

            return false;
        }

		/// <summary>
		/// Handles the <see cref="ITransport.MsgReceived"/> event from the <see cref="ITransport"/> used
		/// for the bus.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">The arguments for the event.</param>
		/// <remarks>
		/// When the transport passes up the <see cref="Msg"/> its received,
		/// the bus checks for initializiation, 
		/// sets the message as that which is currently being handled for the current thread
		/// and, depending on <see cref="DisableMessageHandling"/>, attempts to handle the message.
		/// </remarks>
        private void transport_MsgReceived(object sender, MsgReceivedEventArgs e)
        {
            Msg msg = e.Message;

            if (IsInitializationMessage(msg))
            {
                log.Info(this.transport.Address + " initialized.");
                return;
            }

            try
            {
                log.Debug("Received message. First element of type: " + msg.Body[0].GetType());

                messageBeingHandled = msg;

                if (this.MessageReceived != null)
                    this.MessageReceived(msg);

                if (!this.disableMessageHandling)
                    this.HandleMessage(msg);

                log.Debug("Finished handling message.");
            }
            catch (Exception ex)
            {
                log.Error("Failed handling message.", GetInnermostException(ex));
                throw;
            }
        }

		/// <summary>
		/// Checks whether a received message is an initialization message.
		/// </summary>
		/// <param name="msg">The message to check.</param>
		/// <returns>true if the message is an initialization message, otherwise false.</returns>
		/// <remarks>
		/// A <see cref="CompletionMessage"/> is used out of convenience as the initialization message.</remarks>
        private bool IsInitializationMessage(Msg msg)
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

            this.messageTypes.Add(typeof(CompletionMessage));
            this.messageTypes.Add(typeof(SubscriptionMessage));
            this.messageTypes.Add(typeof(ReadyMessage));
            this.messageTypes.Add(typeof(IMessage[]));
        }

        /// <summary>
        /// Sends the Msg to the address found in the field <see cref="forwardReceivedMessagesTo"/>
        /// if it isn't null.
        /// </summary>
        /// <param name="m">The message to forward</param>
        private void ForwardMessageIfNecessary(Msg m)
        {
            if (this.forwardReceivedMessagesTo != null)
                this.transport.Send(m, this.forwardReceivedMessagesTo);
        }

		/// <summary>
		/// Requeues a message to be handled later.
		/// </summary>
		/// <param name="m">The message to requeue.</param>
        private void HandleMsgLater(Msg m)
        {
            if (this.distributorDataAddress != null)
                if (messageBeingHandled != null)
                    if (messageBeingHandled.Body == m.Body)
                    {
                        this.transport.Send(m, this.distributorDataAddress);
                        return;
                    }

            this.transport.ReceiveMessageLater(m);
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

            if (messageType.IsAbstract)
                return;

            if (typeof(IMessage).IsAssignableFrom(messageType))
            {
                if (this.MustNotOverrideExistingConfiguration(messageType, configuredByAssembly))
                    return;

                this.messageTypeToDestinationLookup[messageType] = destination;
                this.messageTypes.Add(messageType);

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
            return this.messageTypeToDestinationLookup.ContainsKey(messageType) && configuredByAssembly;
        }

		/// <summary>
		/// Wraps the provided messages in an NServiceBus envelope.
		/// </summary>
		/// <param name="messages">The messages to wrap.</param>
		/// <returns>The envelope containing the messages.</returns>
        protected Msg GetMsgFor(params IMessage[] messages)
        {
            Msg result = new Msg();
            result.Body = messages;
            result.ReturnAddress = this.transport.Address;
            result.WindowsIdentityName = Thread.CurrentPrincipal.Identity.Name;

            if (this.propogateReturnAddressOnSend)
                result.ReturnAddress = this.transport.Address;

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

		/// <summary>
		/// Evaluates a type and loads it if it implements IMessageHander<T>.
		/// </summary>
		/// <param name="t">The type to evaluate.</param>
        private void If_Type_Is_MessageHandler_Then_Load(Type t)
        {
            if (t.IsAbstract)
                return;

            Type parent = t.BaseType;
            while (parent != typeof(Object))
            {
                if (parent.IsGenericType)
                {
                    Type[] args = parent.GetGenericArguments();
                    if (args.Length != 1)
                        continue;

                    if (!typeof(IMessage).IsAssignableFrom(args[0]))
                        continue;

                    Type handlerType = typeof(IMessageHandler<>).MakeGenericType(args[0]);
                    if (handlerType.IsAssignableFrom(parent))
                    {
                        this.RegisterHandlerTypeForMessageType(t, args[0]);
                        break;
                    }
                }

                parent = parent.BaseType;
            }
        }

		/// <summary>
		/// Registers a relationship between a message type and a handler for that type.
		/// </summary>
		/// <param name="handlerType">The type of the handler.</param>
		/// <param name="messageType">The type of the message to associate with the handler.</param>
        private void RegisterHandlerTypeForMessageType(Type handlerType, Type messageType)
        {
            if (!this.messageTypeToHandlerTypeLookup.ContainsKey(messageType))
                this.messageTypeToHandlerTypeLookup.Add(messageType, new List<Type>());
                
            if (!this.messageTypeToHandlerTypeLookup[messageType].Contains(handlerType))
            {
                this.messageTypeToHandlerTypeLookup[messageType].Add(handlerType);
                if (log.IsDebugEnabled)
                    log.Debug(string.Format("Associated '{0}' message with '{1}' handler", messageType, handlerType));
            }
        }

		/// <summary>
		/// Gets a list of handler types associated with a message type.
		/// </summary>
		/// <param name="messageType">The type of message to get the handlers for.</param>
		/// <returns>The list of handler types associated with the message type.</returns>
        private IList<Type> GetHandlerTypes(Type messageType)
        {
            IList<Type> result;
            this.messageTypeToHandlerTypeLookup.TryGetValue(messageType, out result);

            if (result == null)
                result = new List<Type>();

            return result;
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

			//WHY: Shouldn't a null check be done here?
			if (destination == null)
				throw new ConfigurationException(
					string.Format("No destination could be found for message type {0}.", messageType)
					);

            return destination;
        }

        #endregion

        #region Fields

		/// <summary>
		/// Gets/sets the subscription manager to use for the bus.
		/// </summary>
        protected SubscriptionsManager subscriptionsManager = new SubscriptionsManager();

        List<Type> messageTypes = new List<Type>();
        IDictionary<Type, IList<Type>> messageTypeToHandlerTypeLookup = new Dictionary<Type, IList<Type>>();
        IDictionary<string, BusAsyncResult> messageIdToAsyncResultLookup = new Dictionary<string, BusAsyncResult>();

        /// <remarks>
        /// Accessed by multiple threads - needs appropriate locking
		/// </remarks>
        IDictionary<Type, string> messageTypeToDestinationLookup = new Dictionary<Type, string>();

		/// <remarks>
        /// ThreadStatic
		/// </remarks>
        [ThreadStatic]
        static Msg messageBeingHandled;

        private static ILog log = LogManager.GetLogger(typeof(UnicastBus));
        #endregion
    }
}
