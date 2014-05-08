namespace NServiceBus.AcceptanceTests.PubSub
{
    using System;
    using Features;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    [Serializable]
    public class Subscriptions
    {
        public static Action<Action<SubscriptionEventArgs>> OnEndpointSubscribed = actionToPerform =>
            {
                if (Feature.IsEnabled<MessageDrivenSubscriptions>())
                {
                    Configure.Instance.Builder.Build<MessageDrivenSubscriptionManager>().ClientSubscribed +=
                        (sender, args) =>
                        {
                            actionToPerform(args);
                        };
                }
            };
    }
}