namespace NServiceBus.Transport.ActiveMQ
{
    public interface ITopicEvaluator
    {
        string GetTopicFromMessageType(string typeNames);
    }
}