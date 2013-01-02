namespace NServiceBus.Transport.ActiveMQ
{
    using System;
    using System.Linq;

    public class TopicEvaluator : ITopicEvaluator
    {
        public string GetTopicFromMessageType(string typeName)
        {
            var type = typeName
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(Type.GetType)
                .First(t => t != null);
            return "VirtualTopic." + type.Name;
        }
    }
}