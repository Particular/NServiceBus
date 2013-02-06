namespace NServiceBus.Transport.ActiveMQ
{
    using System;

    public interface ITopicEvaluator
    {
        string GetTopicFromMessageType(Type type);
    }
}