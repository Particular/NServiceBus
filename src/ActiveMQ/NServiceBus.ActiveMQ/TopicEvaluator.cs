namespace NServiceBus.Transport.ActiveMQ
{
    using System;
    using System.Linq;

    public class TopicEvaluator : ITopicEvaluator
    {
        public string GetTopicFromMessageType(Type type)
        {
            return "VirtualTopic." + type.Name;
        }
    }
}