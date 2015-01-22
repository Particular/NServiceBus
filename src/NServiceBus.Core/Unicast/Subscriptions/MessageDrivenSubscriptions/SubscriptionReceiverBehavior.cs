namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Logging;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

    class SubscriptionReceiverBehavior : PhysicalMessageProcessingStageBehavior
    {
        public ISubscriptionStorage SubscriptionStorage { get; set; }

        public IAuthorizeSubscriptions SubscriptionAuthorizer
        {
            get { return subscriptionAuthorizer ?? (subscriptionAuthorizer = new NoopSubscriptionAuthorizer()); }
            set { subscriptionAuthorizer = value; }
        }

        public override void Invoke(Context context, Action next)
        {
            var transportMessage = context.PhysicalMessage;
            var messageTypeString = GetSubscriptionMessageTypeFrom(transportMessage);

            var intent = transportMessage.MessageIntent;

            if (string.IsNullOrEmpty(messageTypeString) && intent != MessageIntentEnum.Subscribe && intent != MessageIntentEnum.Unsubscribe)
            {
                next();
                return;
            }

            if (string.IsNullOrEmpty(messageTypeString))
            {
                throw new InvalidOperationException("Message intent is Subscribe, but the subscription message type header is missing!");
            }

            if (intent != MessageIntentEnum.Subscribe && intent != MessageIntentEnum.Unsubscribe)
            {
                throw new InvalidOperationException("Subscription messages need to have intent set to Subscribe/Unsubscribe");
            }

            var subscriberAddress = transportMessage.ReplyToAddress;

            if (subscriberAddress == null || subscriberAddress == Address.Undefined)
            {
                throw new InvalidOperationException("Subscription message arrived without a valid ReplyToAddress");
            }


            if (SubscriptionStorage == null)
            {
                var warning = string.Format("Subscription message from {0} arrived at this endpoint, yet this endpoint is not configured to be a publisher. To avoid this warning make this endpoint a publisher by configuring a subscription storage or using the AsA_Publisher role.", subscriberAddress);
                Logger.WarnFormat(warning);

                if (Debugger.IsAttached) // only under debug, so that we don't expose ourselves to a denial of service
                {
                    throw new InvalidOperationException(warning); // and cause message to go to error queue by throwing an exception
                }

                return;
            }

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

                    SubscriptionStorage.Subscribe(transportMessage.ReplyToAddress, new[]
                    {
                        mt
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
            SubscriptionStorage.Unsubscribe(subscriberAddress, new[]
            {
                new MessageType(messageTypeString)
            });
        }


        static string GetSubscriptionMessageTypeFrom(TransportMessage msg)
        {
            return (from header in msg.Headers where header.Key == Headers.SubscriptionMessageType select header.Value).FirstOrDefault();
        }

        static ILog Logger = LogManager.GetLogger<SubscriptionReceiverBehavior>();
        IAuthorizeSubscriptions subscriptionAuthorizer;
    }
}