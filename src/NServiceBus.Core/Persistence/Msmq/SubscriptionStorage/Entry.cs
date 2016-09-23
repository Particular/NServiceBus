namespace NServiceBus
{
    using System;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class Entry
    {
        public MessageType MessageType { get; set; }

        public Subscriber Subscriber { get; set; }

        public DateTime Subscribed { get; set; }
        public string MessageId { get; set; }
    }
}