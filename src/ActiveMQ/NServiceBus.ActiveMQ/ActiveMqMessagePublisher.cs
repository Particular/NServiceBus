namespace NServiceBus.Transports.ActiveMQ
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ActiveMqMessagePublisher : IPublishMessages
    {
        private readonly ITopicEvaluator topicEvaluator;
        private readonly IMessageProducer messageProducer;

        public ActiveMqMessagePublisher(ITopicEvaluator topicEvaluator, IMessageProducer messageProducer)
        {
            this.topicEvaluator = topicEvaluator;
            this.messageProducer = messageProducer;
        }

        public bool Publish(TransportMessage message, IEnumerable<Type> eventTypes)
        {
            var eventType = eventTypes.First(); //we route on the first event for now

            var topic = topicEvaluator.GetTopicFromMessageType(eventType);
            messageProducer.SendMessage(message, topic, "topic://");

            return true;
        }
    }
}