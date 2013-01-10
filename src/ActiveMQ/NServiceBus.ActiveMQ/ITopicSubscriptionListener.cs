namespace NServiceBus.Transport.ActiveMQ
{
    public interface ITopicSubscriptionListener
    {
        void TopicSubscribed(object sender, SubscriptionEventArgs e);
        void TopicUnsubscribed(object sender, SubscriptionEventArgs e);
    }
}