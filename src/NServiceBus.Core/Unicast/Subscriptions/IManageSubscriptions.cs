namespace NServiceBus.Unicast.Subscriptions
{
    using System;

    public interface IManageSubscriptions
    {
        void Subscribe(Type eventType, Address publisherAddress);
        void Unsubscribe(Type eventType, Address publisherAddress);
    }
}