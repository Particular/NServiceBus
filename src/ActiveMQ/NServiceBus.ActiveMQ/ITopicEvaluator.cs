namespace NServiceBus.Transports.ActiveMQ
{
    using System;

    public interface ITopicEvaluator
    {
        string GetTopicFromMessageType(Type type);
    }
}