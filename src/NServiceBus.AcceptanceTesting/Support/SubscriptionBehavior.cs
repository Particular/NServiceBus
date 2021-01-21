namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class SubscriptionBehavior<TContext> : IBehavior<ITransportReceiveContext, ITransportReceiveContext> where TContext : ScenarioContext
    {
        public SubscriptionBehavior(Action<SubscriptionEventArgs, TContext> action, TContext scenarioContext, MessageIntentEnum intentToHandle)
        {
            this.action = action;
            this.scenarioContext = scenarioContext;
            this.intentToHandle = intentToHandle;
        }

        public async Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, CancellationToken, Task> next, CancellationToken token)
        {
            await next(context, token).ConfigureAwait(false);
            var subscriptionMessageType = GetSubscriptionMessageTypeFrom(context.Message);
            if (subscriptionMessageType != null)
            {
                if (!context.Message.Headers.TryGetValue(Headers.SubscriberTransportAddress, out var returnAddress))
                {
                    context.Message.Headers.TryGetValue(Headers.ReplyToAddress, out returnAddress);
                }

                if (!context.Message.Headers.TryGetValue(Headers.SubscriberEndpoint, out var endpointName))
                {
                    endpointName = string.Empty;
                }

                var intent = (MessageIntentEnum)Enum.Parse(typeof(MessageIntentEnum), context.Message.Headers[Headers.MessageIntent], true);
                if (intent != intentToHandle)
                {
                    return;
                }

                action(new SubscriptionEventArgs
                {
                    MessageType = subscriptionMessageType,
                    SubscriberReturnAddress = returnAddress,
                    SubscriberEndpoint = endpointName
                }, scenarioContext);
            }
        }

        static string GetSubscriptionMessageTypeFrom(IncomingMessage msg)
        {
            return msg.Headers.TryGetValue(Headers.SubscriptionMessageType, out var headerValue) ? headerValue : null;
        }

        Action<SubscriptionEventArgs, TContext> action;
        TContext scenarioContext;
        MessageIntentEnum intentToHandle;
    }
}