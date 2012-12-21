namespace NServiceBus.Unicast.Subscriptions
{
    using System;

    public interface IManageSubscriptions
    {
        void Subscribe(Type eventType, Address publisherAddress, Predicate<object> condition);
        void Unsubscribe(Type eventType, Address publisherAddress);
        
        event EventHandler<SubscriptionEventArgs> ClientSubscribed;
    }
}