namespace NServiceBus
{
    using System;
    using System.Messaging;

    class MsmqSubscriptionMessage
    {
        public MsmqSubscriptionMessage(Message m)
        {
            Body = m.Body;
            Label = m.Label;
            Id = m.Id;
            ArrivedTime = m.ArrivedTime;
        }

        public MsmqSubscriptionMessage()
        {
        }

        public DateTime ArrivedTime { get; set; }

        public object Body { get; set; }

        public string Label { get; set; }

        public string Id { get; set; }
    }
}