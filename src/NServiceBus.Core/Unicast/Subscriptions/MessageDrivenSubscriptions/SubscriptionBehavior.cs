namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    class SubscriptionBehavior: IBehavior<IncomingContext>
    {
        MessageDrivenSubscriptionManager subscriptionManager;

        public SubscriptionBehavior(MessageDrivenSubscriptionManager subscriptionManager)
        {
            this.subscriptionManager = subscriptionManager;
        }

        public void Invoke(IncomingContext context, Action next)
        {
            if (!subscriptionManager.TryHandleMessage(context.PhysicalMessage))
            {
                next();
            }
        }
    }
}