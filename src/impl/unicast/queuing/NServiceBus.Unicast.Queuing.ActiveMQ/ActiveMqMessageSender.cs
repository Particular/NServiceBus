namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    using System;
    using Apache.NMS;
    using Apache.NMS.Util;

    public class ActiveMqMessageSender : ISendMessages
    {
        private readonly INetTxConnection connection;
        private readonly ISubscriptionManager subscriptionManager;
        private readonly IActiveMqMessageMapper activeMqMessageMapper;
        private readonly ITopicEvaluator topicEvaluator;

        public ActiveMqMessageSender(
            INetTxConnection connection, 
            ISubscriptionManager subscriptionManager, 
            IActiveMqMessageMapper activeMqMessageMapper,
            ITopicEvaluator topicEvaluator)
        {
            this.connection = connection;
            this.subscriptionManager = subscriptionManager;
            this.activeMqMessageMapper = activeMqMessageMapper;
            this.topicEvaluator = topicEvaluator;
        }

        public void Send(TransportMessage message, Address address)
        {
            switch (message.MessageIntent)
            {
                case MessageIntentEnum.Subscribe:
                    this.Subscribe(message);
                    break;
                case MessageIntentEnum.Unsubscribe:
                    this.Unsubscribe(message);
                    break;
                case MessageIntentEnum.Publish:
                    this.PublishMessage(message, address);
                    break;
                default:
                    this.SendMessage(message, address);
                    break;
            }
        }

        private void Subscribe(TransportMessage message)
        {
            var messageType = message.Headers[UnicastBus.SubscriptionMessageType];
            var topic = this.topicEvaluator.GetTopicFromMessageType(messageType);

            lock (this.subscriptionManager)
            {
                this.subscriptionManager.Subscribe(topic);
            }
        }

        private void Unsubscribe(TransportMessage message)
        {
            var messageType = message.Headers[UnicastBus.SubscriptionMessageType];
            var topic = this.topicEvaluator.GetTopicFromMessageType(messageType);

            lock (this.subscriptionManager)
            {
                this.subscriptionManager.Unsubscribe(topic);
            }
        }

        private void PublishMessage(TransportMessage message, Address address)
        {
            if (message.Headers == null || !message.Headers.ContainsKey(Headers.EnclosedMessageTypes))
            {
                throw new ArgumentException("Messages must have the enclosed message type on the header.");
            }

            var typeName = message.Headers[Headers.EnclosedMessageTypes];
            var topic = this.topicEvaluator.GetTopicFromMessageType(typeName);

            this.SendMessage(message, "topic://" + topic);
        }

        private void SendMessage(TransportMessage message, Address address)
        {
            this.SendMessage(message, "queue://" + address.Queue);
        }

        private void SendMessage(TransportMessage message, string destination)
        {
            using (var session = this.connection.CreateNetTxSession())
            {
                var jmsMessage = this.activeMqMessageMapper.CreateJmsMessage(message, session);

                var d = SessionUtil.GetDestination(session, destination);
                using (var producer = session.CreateProducer())
                {
                    producer.Send(d, jmsMessage);
                }
            }
        }
    }
}
