namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;
    using Transport;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class SubscriptionReceiverBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public SubscriptionReceiverBehavior(ISubscriptionStorage subscriptionStorage, Func<IIncomingPhysicalMessageContext, bool> authorizer)
        {
            this.subscriptionStorage = subscriptionStorage;
            this.authorizer = authorizer;
        }

        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            var incomingMessage = context.Message;
            var messageTypeString = GetSubscriptionMessageTypeFrom(incomingMessage);

            var intent = incomingMessage.GetMesssageIntent();

            if (string.IsNullOrEmpty(messageTypeString) && intent != MessageIntentEnum.Subscribe && intent != MessageIntentEnum.Unsubscribe)
            {
                await next(context).ConfigureAwait(false);
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

            string subscriberAddress;
            string subscriberEndpoint = null;

            if (incomingMessage.Headers.TryGetValue(Headers.SubscriberTransportAddress, out subscriberAddress))
            {
                subscriberEndpoint = incomingMessage.Headers[Headers.SubscriberEndpoint];
            }
            else
            {
                subscriberAddress = incomingMessage.GetReplyToAddress();
            }

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

            if (!authorizer(context))
            {
                Logger.Debug($"{intent} from {subscriberAddress} on message type {messageTypeString} was refused.");
                return;
            }
            Logger.Info($"{intent} from {subscriberAddress} on message type {messageTypeString}");
            var subscriber = new Subscriber(subscriberAddress, subscriberEndpoint);
            if (incomingMessage.GetMesssageIntent() == MessageIntentEnum.Subscribe)
            {
                var messageType = new MessageType(messageTypeString);
                await subscriptionStorage.Subscribe(subscriber, messageType, context.Extensions).ConfigureAwait(false);
                return;
            }

            await subscriptionStorage.Unsubscribe(subscriber, new MessageType(messageTypeString), context.Extensions).ConfigureAwait(false);
        }

        static string GetSubscriptionMessageTypeFrom(IncomingMessage msg)
        {
            string value;
            msg.Headers.TryGetValue(Headers.SubscriptionMessageType, out value);
            return value;
        }

        Func<IIncomingPhysicalMessageContext, bool> authorizer;

        ISubscriptionStorage subscriptionStorage;

        static ILog Logger = LogManager.GetLogger<SubscriptionReceiverBehavior>();

        public class Registration : RegisterStep
        {
            public Registration()
                : base("ProcessSubscriptionRequests", typeof(SubscriptionReceiverBehavior), "Check for subscription messages and execute the requested behavior to subscribe or unsubscribe.")
            {
                InsertAfterIfExists("ExecuteUnitOfWork");
            }
        }
    }
}