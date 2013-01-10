﻿namespace NServiceBus.Transport.ActiveMQ
{
    using NServiceBus.Unicast.Queuing;

    public class ActiveMqMessageSender : ISendMessages
    {
        private readonly IMessageProducer messageProducer;

        public ActiveMqMessageSender(IMessageProducer messageProducer)
        {
            this.messageProducer = messageProducer;
        }

        public void Send(TransportMessage message, Address address)
        {
            this.messageProducer.SendMessage(message, address.Queue, "queue://");
        }
    }
}
