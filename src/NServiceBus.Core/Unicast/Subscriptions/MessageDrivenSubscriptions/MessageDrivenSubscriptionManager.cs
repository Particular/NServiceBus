namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using Logging;
    using MessageMutator;
    using ObjectBuilder;
    using Queuing;
    using Transport;
    using Transports;

    /// <summary>
    /// Implements message driven subscriptions for transports that doesn't have native support for it (MSMQ , SqlServer, Azure Queues etc)
    /// </summary>
    public class MessageDrivenSubscriptionManager : IManageSubscriptions,
                                                    IMutateIncomingTransportMessages
                                                    
                                                    
    {
        public ISendMessages MessageSender { get; set; }
        public IBuilder Builder { get; set; }
        public ISubscriptionStorage SubscriptionStorage { get; set; }
        public IAuthorizeSubscriptions SubscriptionAuthorizer { get { return subscriptionAuthorizer ?? (subscriptionAuthorizer = new NoopSubscriptionAuthorizer()); } set { subscriptionAuthorizer = value; } }

        public void Subscribe(Type eventType, Address publisherAddress)
        {
            if (publisherAddress == Address.Undefined)
                throw new InvalidOperationException(string.Format("No destination could be found for message type {0}. Check the <MessageEndpointMappings> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.", eventType));

            Logger.Info("Subscribing to " + eventType.AssemblyQualifiedName + " at publisher queue " + publisherAddress);

            var subscriptionMessage = CreateControlMessage(eventType);
            subscriptionMessage.MessageIntent = MessageIntentEnum.Subscribe;

            ThreadPool.QueueUserWorkItem(state =>
                                         SendSubscribeMessageWithRetries(publisherAddress, subscriptionMessage, eventType.AssemblyQualifiedName));
        }


        public void Unsubscribe(Type eventType, Address publisherAddress)
        {
            if (publisherAddress == Address.Undefined)
                throw new InvalidOperationException(string.Format("No destination could be found for message type {0}. Check the <MessageEndpointMapping> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.", eventType));
          
            Logger.Info("Unsubscribing from " + eventType.AssemblyQualifiedName + " at publisher queue " + publisherAddress);

            var subscriptionMessage = CreateControlMessage(eventType);
            subscriptionMessage.MessageIntent = MessageIntentEnum.Unsubscribe;

            MessageSender.Send(subscriptionMessage, publisherAddress);
        }

        public event EventHandler<SubscriptionEventArgs> ClientSubscribed;

        public void MutateIncoming(TransportMessage transportMessage)
        {
           var messageTypeString = GetSubscriptionMessageTypeFrom(transportMessage);

            var intent = transportMessage.MessageIntent;

            if (string.IsNullOrEmpty(messageTypeString) && intent != MessageIntentEnum.Subscribe && intent != MessageIntentEnum.Unsubscribe)
                return;

            if (string.IsNullOrEmpty(messageTypeString))
                throw new InvalidOperationException("Message intent is Subscribe, but the subscription message type header is missing!");

            if (intent != MessageIntentEnum.Subscribe && intent != MessageIntentEnum.Unsubscribe)
                throw new InvalidOperationException("Subscription messages need to have intent set to Subscribe/Unsubscribe");

            var subscriberAddress = transportMessage.ReplyToAddress;

            if (subscriberAddress == null || subscriberAddress == Address.Undefined)
                throw new InvalidOperationException("Subscription message arrived without a valid ReplyToAddress");


            if (SubscriptionStorage == null)
            {
                var warning = string.Format("Subscription message from {0} arrived at this endpoint, yet this endpoint is not configured to be a publisher. To avoid this warning make this endpoint a publisher by configuring a subscription storage or using the AsA_Publisher role.", subscriberAddress);
                Logger.WarnFormat(warning);

                if (Debugger.IsAttached) // only under debug, so that we don't expose ourselves to a denial of service
                    throw new InvalidOperationException(warning);  // and cause message to go to error queue by throwing an exception

                return;
            }

            //service locate to avoid a circular dependency
            Builder.Build<IBus>().DoNotContinueDispatchingCurrentMessageToHandlers();

            if (transportMessage.MessageIntent == MessageIntentEnum.Subscribe)
            {
                if (!SubscriptionAuthorizer.AuthorizeSubscribe(messageTypeString, subscriberAddress.ToString(), transportMessage.Headers))
                {
                    Logger.Debug(string.Format("Subscription request from {0} on message type {1} was refused.", subscriberAddress, messageTypeString));
                }
                else
                {
                    Logger.Info("Subscribing " + subscriberAddress + " to message type " + messageTypeString);

                    var mt = new MessageType(messageTypeString);

                    SubscriptionStorage.Subscribe(transportMessage.ReplyToAddress, new[] { mt });
                    if (ClientSubscribed != null)
                        ClientSubscribed(this, new SubscriptionEventArgs
                                             {
                                                 MessageType = messageTypeString,
                                                 SubscriberReturnAddress = subscriberAddress
                                             });
                }

                return;
            }


            if (!SubscriptionAuthorizer.AuthorizeUnsubscribe(messageTypeString, subscriberAddress.ToString(), transportMessage.Headers))
            {
                Logger.Debug(string.Format("Unsubscribe request from {0} on message type {1} was refused.", subscriberAddress, messageTypeString));
                return;
            }

            Logger.Info("Unsubscribing " + subscriberAddress + " from message type " + messageTypeString);
            SubscriptionStorage.Unsubscribe(subscriberAddress, new[] { new MessageType(messageTypeString) });
        }

    


        static string GetSubscriptionMessageTypeFrom(TransportMessage msg)
        {
            return (from header in msg.Headers where header.Key == Headers.SubscriptionMessageType select header.Value).FirstOrDefault();
        }

        static TransportMessage CreateControlMessage(Type eventType)
        {
            var subscriptionMessage = ControlMessage.Create(Address.Local);

            subscriptionMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
            return subscriptionMessage;
        }

        void SendSubscribeMessageWithRetries(Address destination, TransportMessage subscriptionMessage, string messageType, int retriesCount = 0)
        {
            try
            {

                MessageSender.Send(subscriptionMessage, destination);
            }
            catch (QueueNotFoundException ex)
            {
                if (retriesCount < 10)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                    SendSubscribeMessageWithRetries(destination, subscriptionMessage, messageType, ++retriesCount);
                }
                else
                {
                    Logger.ErrorFormat("Failed to subscribe to {0} at publisher queue {1}", ex, messageType, destination);
                }
            }
        }

     

    
     
        IAuthorizeSubscriptions subscriptionAuthorizer;

        readonly static ILog Logger = LogManager.GetLogger(typeof(MessageDrivenSubscriptionManager));
    }

    class StorageInitializer : IWantToRunWhenBusStartsAndStops
    {
        public ISubscriptionStorage SubscriptionStorage { get; set; }

        public void Start()
        {
            if (SubscriptionStorage != null)
                SubscriptionStorage.Init();
        }

        public void Stop()
        {

        }

    }
}