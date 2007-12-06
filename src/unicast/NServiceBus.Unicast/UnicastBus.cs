using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using Common.Logging;
using NServiceBus.Async;
using NServiceBus.Messages;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Transport;
using ObjectBuilder;

namespace NServiceBus.Unicast
{
    public class UnicastBus : IBus
    {
        #region config properties

        private bool disableMessageHandling = false;
        public bool DisableMessageHandling
        {
            set { disableMessageHandling = value; }
        }

        protected ITransport transport;
        public ITransport Transport
        {
            set
            {
                transport = value;

                this.transport.MsgReceived += transport_MsgReceived;
            }
        }

        public delegate void MessageReceivedDelegate(Msg message);
        public event MessageReceivedDelegate MessageReceived;

        public ISubscriptionStorage SubscriptionStorage
        {
            set
            {
                this.subscriptionsManager.Storage = value;
            }
        }

        private IBuilder builder;
        public IBuilder Builder
        {
            set { builder = value; }
        }

        private bool propogateReturnAddressOnSend = false;
        public bool PropogateReturnAddressOnSend
        {
            set { propogateReturnAddressOnSend = value; }
        }

        private bool impersonateSender;
        public bool ImpersonateSender
        {
            set { impersonateSender = value; }
        }

        private string distributorDataAddress;
        public string DistributorDataAddress
        {
            set { distributorDataAddress = value; }
        }

        public IDictionary MessageOwners
        {
            get
            {
                return null;
            }
            set
            {
                foreach (DictionaryEntry de in value)
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

                this.messageTypes.Add(typeof(ErrorMessage));
                this.messageTypes.Add(typeof(SubscriptionMessage));
                this.messageTypes.Add(typeof(ReadyMessage));
                this.messageTypes.Add(typeof(IMessage[]));
            }
        }

        /// <summary>
        /// List of strings, where each string is the name of an assembly
        /// </summary>
        public IList MessageHandlerAssemblies
        {
            get { return null; }
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

        public virtual void Publish(IMessage message)
        {
            this.Publish(new IMessage[] { message });
        }

        public virtual void Publish(params IMessage[] messages)
        {
            foreach (string subscriber in this.subscriptionsManager.GetSubscribersForMessage(messages[0]))
                try
                {
                    this.Send(messages, subscriber);
                }
                catch(Exception e)
                {
                    log.Error("Problem sending message to subscriber: " + subscriber, e);
                }
        }

        public virtual void Subscribe(Type messageType)
        {
            this.Subscribe(messageType, null);
        }

        public virtual void Subscribe(Type messageType, Predicate<IMessage> condition)
        {
            this.subscriptionsManager.AddConditionForSubscriptionToMessageType(messageType, condition);

            string destination = this.GetDestinationForMessageType(messageType);

            this.Send(new SubscriptionMessage(messageType.AssemblyQualifiedName, SubscriptionType.Add), destination);
        }

        public virtual void Unsubscribe(Type messageType)
        {
            string destination = this.GetDestinationForMessageType(messageType);

            this.Send(new SubscriptionMessage(messageType.AssemblyQualifiedName, SubscriptionType.Remove), destination);
        }

        public void Reply(IMessage message)
        {
            this.Reply(new IMessage[] { message });
        }

        public void Reply(params IMessage[] messages)
        {
            Msg toSend = this.GetMsgFor(messages);

            toSend.CorrelationId = messageBeingHandled.Id;

            this.transport.Send(toSend, messageBeingHandled.ReturnAddress);
        }

        public void Return(int errorCode)
        {
            ErrorMessage msg = new ErrorMessage();
            msg.ErrorCode = errorCode;

            this.Reply(msg);
        }

        public void HandleCurrentMessageLater()
        {
            HandleMsgLater(messageBeingHandled);
        }

        public void HandleMessagesLater(params IMessage[] messages)
        {
            Msg m = this.GetMsgFor(messages);

            this.HandleMsgLater(m);
        }

        public void Send(IMessage message)
        {
            this.Send(new IMessage[] { message });
        }

        public void Send(params IMessage[] messages)
        {
            CompletionCallback callback = null;
            object state = null;

            this.Send(messages, callback, state);
        }

        public void SendLocal(params IMessage[] messages)
        {
            Msg m = this.GetMsgFor(messages);

            this.transport.ReceiveMessageLater(m);
        }

        public void Send(IMessage message, string destination)
        {
            this.Send(message, destination, null, null);
        }

        public void Send(IMessage[] messages, string destination)
        {
            this.Send(messages, destination, null, null);
        }

        public void Send(IMessage message, CompletionCallback callback, object state)
        {
            string destination = this.messageTypeToDestinationLookup[message.GetType()];

            this.Send(message, destination, callback, state);
        }

        public void Send(IMessage[] messages, CompletionCallback callback, object state)
        {
            string destination = this.messageTypeToDestinationLookup[messages[0].GetType()];

            this.Send(messages, destination, callback, state);
        }

        public void Send(IMessage message, string destination, CompletionCallback callback, object state)
        {
            this.Send(new IMessage[] { message }, destination, callback, state);
        }

        public void Send(IMessage[] messages, string destination, CompletionCallback callback, object state)
        {
            Msg toSend = this.GetMsgFor(messages);
            this.transport.Send(toSend, destination);

            if (callback != null)
                lock (this.messageIdToCallbackLookup)
                    this.messageIdToCallbackLookup[toSend.Id] = new CallbackHolder(callback, state);
        }

        public string SourceOfMessageBeingHandled
        {
            get
            {
                if (messageBeingHandled != null)
                    return messageBeingHandled.ReturnAddress;

                return null;
            }
        }

        public virtual void Start()
        {
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            this.transport.MessageTypesToBeReceived = this.messageTypes;

            this.transport.Start();

            this.SendLocal(new ErrorMessage());
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            doNotContinueDispatchingCurrentMessageToHandlers = true;
        }

        [ThreadStatic]
        private static bool doNotContinueDispatchingCurrentMessageToHandlers = false;

        #endregion

        #region receiving and handling

        //private void ReceiveAndHandle()
        //{
        //    if (this.distributorControlAddress != null)
        //        if (!sentReadyMessage)
        //        {
        //            this.Send(new Messages.ReadyMessage(), this.distributorControlAddress);
        //            sentReadyMessage = true;
        //        }

        //    Msg msg = this.transport.Receive(this.timeToWaitForReceive);
        //    if (msg == null)
        //        return;


        //}

        // run by multiple threads so must be thread safe
        // public for testing
        public void HandleMessage(Msg m)
        {
            if (this.HandledErrorMessage(m))
                return;

            if (this.subscriptionsManager.HandledSubscriptionMessage(m))
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

        private bool DispatchMessageToHandlersBasedOnType(IMessage toHandle, Type messageType)
        {
            foreach (Type messageHandlerType in this.GetHandlerTypes(messageType))
            {
                try
                {
                    log.Debug("Activating: " + messageHandlerType.Name);

                    this.builder.BuildAndDispatch(messageHandlerType, "Handle", toHandle);
                    
                    log.Debug(messageHandlerType.Name + " Done.");

                    if (!doNotContinueDispatchingCurrentMessageToHandlers)
                        return false;
                }
                catch (Exception e)
                {
                    log.Error(messageHandlerType.Name + " Failed handling message.", this.GetInnermostException(e));

                    throw;
                }
            }

            return true;
        }

        private Exception GetInnermostException(Exception e)
        {
            Exception result = e;
            while (result.InnerException != null)
                result = result.InnerException;

            return result;
        }

        private bool HandledErrorMessage(Msg msg)
        {
            if (msg.Body.Length != 1)
                return false;

            ErrorMessage errorMessage = msg.Body[0] as ErrorMessage;
            if (errorMessage != null)
            {
                CallbackHolder callbackHolder;

                lock (this.messageIdToCallbackLookup)
                {
                    this.messageIdToCallbackLookup.TryGetValue(msg.CorrelationId, out callbackHolder);
                    this.messageIdToCallbackLookup.Remove(msg.CorrelationId);
                }

                if (callbackHolder != null)
                    callbackHolder.Callback(errorMessage.ErrorCode, callbackHolder.State);

                return true;
            }

            return false;
        }

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
                log.Error("Failed handling message.", this.GetInnermostException(ex));
            }
        }

        private bool IsInitializationMessage(Msg msg)
        {
            if (!msg.ReturnAddress.Contains(this.transport.Address))
                return false;

            if (msg.CorrelationId != null)
                return false;

            if (msg.Body.Length > 1)
                return false;

            ErrorMessage em = msg.Body[0] as ErrorMessage;
            if (em == null)
                return false;

            return true;
        }

        #endregion

        internal class CallbackHolder
        {
            public CallbackHolder(CompletionCallback callback, object state)
            {
                Callback = callback;
                State = state;
            }

            public CompletionCallback Callback;
            public object State;
        }

        #region helper methods

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

        public void AddTypesFromAssembly(Assembly a)
        {
            foreach (Type t in a.GetTypes())
            {
                if (typeof(IMessage).IsAssignableFrom(t) && !t.IsAbstract)
                {
                    this.messageTypes.Add(t);
                    continue;
                }

                If_Type_Is_MessageHandler_Then_Load(t);
            }
        }

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

        private bool MustNotOverrideExistingConfiguration(Type messageType, bool configuredByAssembly)
        {
            return this.messageTypeToDestinationLookup.ContainsKey(messageType) && configuredByAssembly;
        }

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

        private void RegisterHandlerTypeForMessageType(Type handlerType, Type messageType)
        {
            if (!this.messageTypeToHandlerTypeLookup.ContainsKey(messageType))
                this.messageTypeToHandlerTypeLookup.Add(messageType, new List<Type>());

            if (!this.messageTypeToHandlerTypeLookup[messageType].Contains(handlerType))
                this.messageTypeToHandlerTypeLookup[messageType].Add(handlerType);
        }

        private IList<Type> GetHandlerTypes(Type messageType)
        {
            IList<Type> result;
            this.messageTypeToHandlerTypeLookup.TryGetValue(messageType, out result);

            if (result == null)
                result = new List<Type>();

            return result;
        }

        protected string GetDestinationForMessageType(Type messageType)
        {
            string destination;

            lock (this.messageTypeToDestinationLookup)
                this.messageTypeToDestinationLookup.TryGetValue(messageType, out destination);

            return destination;
        }

        #endregion

        #region Fields

        protected SubscriptionsManager subscriptionsManager = new SubscriptionsManager();

        List<Type> messageTypes = new List<Type>();
        IDictionary<Type, IList<Type>> messageTypeToHandlerTypeLookup = new Dictionary<Type, IList<Type>>();
        IDictionary<string, CallbackHolder> messageIdToCallbackLookup = new Dictionary<string, CallbackHolder>();

        /// <summary>
        /// Accessed by multiple threads - needs appropriate locking
        /// </summary>
        IDictionary<Type, string> messageTypeToDestinationLookup = new Dictionary<Type, string>();

        /// <summary>
        /// ThreadStatic
        /// </summary>
        [ThreadStatic]
        static Msg messageBeingHandled;

        private static ILog log = LogManager.GetLogger(typeof(UnicastBus));
        #endregion
    }
}
