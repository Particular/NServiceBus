﻿namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

    class SubscriptionReceiverBehavior : Behavior<PhysicalMessageProcessingContext>
    {
        public SubscriptionReceiverBehavior(ISubscriptionStorage subscriptionStorage)
        {
            this.subscriptionStorage = subscriptionStorage;
        }

        public override async Task Invoke(PhysicalMessageProcessingContext context, Func<Task> next)
        {
            var incomingMessage = context.Message;
            var messageTypeString = GetSubscriptionMessageTypeFrom(incomingMessage);

            var intent = incomingMessage.GetMesssageIntent();

            if (string.IsNullOrEmpty(messageTypeString) && intent != MessageIntentEnum.Subscribe && intent != MessageIntentEnum.Unsubscribe)
            {
                await next().ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrEmpty(messageTypeString))
            {
                throw new InvalidOperationException("Message intent is Subscribe, but the subscription message type header is missing.");
            }

            if (intent != MessageIntentEnum.Subscribe && intent != MessageIntentEnum.Unsubscribe)
            {
                throw new InvalidOperationException("Subscription messages need to have intent set to Subscribe/Unsubscribe.");
            }

            var subscriberAddress = incomingMessage.GetReplyToAddress();

            if (subscriberAddress == null)
            {
                throw new InvalidOperationException("Subscription message arrived without a valid ReplyToAddress.");
            }

            if (subscriptionStorage == null)
            {
                var warning = $"Subscription message from {subscriberAddress} arrived at this endpoint, yet this endpoint is not configured to be a publisher. To avoid this warning make this endpoint a publisher by configuring a subscription storage or using the AsA_Publisher role.";
                Logger.WarnFormat(warning);

                if (Debugger.IsAttached) // only under debug, so that we don't expose ourselves to a denial of service
                {
                    throw new InvalidOperationException(warning); // and cause message to go to error queue by throwing an exception
                }

                return;
            }

            if (incomingMessage.GetMesssageIntent() == MessageIntentEnum.Subscribe)
            {
                Logger.Info("Subscribing " + subscriberAddress + " to message type " + messageTypeString);

                var mt = new MessageType(messageTypeString);

                await subscriptionStorage.Subscribe(incomingMessage.GetReplyToAddress(), new[]
                {
                    mt
                }, context.Extensions).ConfigureAwait(false);

                return;
            }

            Logger.Info("Unsubscribing " + subscriberAddress + " from message type " + messageTypeString);
            await subscriptionStorage.Unsubscribe(subscriberAddress, new[]
            {
                new MessageType(messageTypeString)
            }, context.Extensions).ConfigureAwait(false);
        }


        static string GetSubscriptionMessageTypeFrom(IncomingMessage msg)
        {
            return (from header in msg.Headers where header.Key == Headers.SubscriptionMessageType select header.Value).FirstOrDefault();
        }

        ISubscriptionStorage subscriptionStorage;

        static ILog Logger = LogManager.GetLogger<SubscriptionReceiverBehavior>();

        public class Registration:RegisterStep
        {
            public Registration()
                : base("ProcessSubscriptionRequests", typeof(SubscriptionReceiverBehavior), "Check for subscription messages and execute the requested behavior to subscribe or unsubscribe.")
            {
                InsertAfterIfExists(WellKnownStep.ExecuteUnitOfWork);
            }
        }
    }
}