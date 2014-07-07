namespace NServiceBus.AcceptanceTests.PubSub
{
    using System;
    using System.Linq;
    using Pipeline;
    using Pipeline.Contexts;

    class SubscriptionBehavior : IBehavior<IncomingContext>
    {
        public void Invoke(IncomingContext context, Action next)
        {
            next();
            var subscriptionMessageType = GetSubscriptionMessageTypeFrom(context.PhysicalMessage);
            if (EndpointSubscribed != null && subscriptionMessageType != null)
            {
                EndpointSubscribed(new SubscriptionEventArgs
                {
                    MessageType = subscriptionMessageType,
                    SubscriberReturnAddress = context.PhysicalMessage.ReplyToAddress
                });
            }
        }

        static string GetSubscriptionMessageTypeFrom(TransportMessage msg)
        {
            return (from header in msg.Headers where header.Key == Headers.SubscriptionMessageType select header.Value).FirstOrDefault();
        }

        public static Action<SubscriptionEventArgs> EndpointSubscribed;

        public static void OnEndpointSubscribed(Action<SubscriptionEventArgs> action)
        {
            EndpointSubscribed = action;
        }

        internal class Registration : RegisterStep
        {
            public Registration() : base("SubscriptionBehavior", typeof(SubscriptionBehavior), "So we can get subscription events")
            {
                InsertBefore(WellKnownStep.CreateChildContainer);
            }
        }
    }
}