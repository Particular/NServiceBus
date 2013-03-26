namespace NServiceBus.Transports.ActiveMQ
{
    using System;

    public class TopicEvaluator : ITopicEvaluator
    {
        public string GetTopicFromMessageType(Type type)
        {
            return "VirtualTopic." + type.FullName;
        }
    }
}