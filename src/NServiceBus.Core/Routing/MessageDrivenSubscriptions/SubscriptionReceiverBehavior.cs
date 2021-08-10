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

            var intent = incomingMessage.GetMessageIntent();

            if (string.IsNullOrEmpty(messageTypeString) && intent != MessageIntent.Subscribe && intent != MessageIntent.Unsubscribe)
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrEmpty(messageTypeString))
            {
                throw new InvalidOperationException("Message intent is Subscribe, but the subscription message type header is missing.");
            }

            if (intent != MessageIntent.Subscribe && intent != MessageIntent.Unsubscribe)
            {
                throw new InvalidOperationException("Subscription messages need to have intent set to Subscribe/Unsubscribe.");
            }

            string subscriberEndpoint = null;

            if (incomingMessage.Headers.TryGetValue(Headers.SubscriberTransportAddress, out var subscriberAddress))
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
                var warning = $"Subscription message from {subscriberAddress} arrived at this endpoint, yet this endpoint is not configured to be a publisher. To avoid this warning make this endpoint a publisher by configuring a subscription storage.";
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
            if (incomingMessage.GetMessageIntent() == MessageIntent.Subscribe)
            {
                var messageType = new MessageType(messageTypeString);
                await subscriptionStorage.Subscribe(subscriber, messageType, context.Extensions, context.CancellationToken).ConfigureAwait(false);
                return;
            }

            await subscriptionStorage.Unsubscribe(subscriber, new MessageType(messageTypeString), context.Extensions, context.CancellationToken).ConfigureAwait(false);
        }

        static string GetSubscriptionMessageTypeFrom(IncomingMessage msg)
        {
            msg.Headers.TryGetValue(Headers.SubscriptionMessageType, out var value);
            return value;
        }

        readonly Func<IIncomingPhysicalMessageContext, bool> authorizer;
        readonly ISubscriptionStorage subscriptionStorage;

        static readonly ILog Logger = LogManager.GetLogger<SubscriptionReceiverBehavior>();
    }
}
