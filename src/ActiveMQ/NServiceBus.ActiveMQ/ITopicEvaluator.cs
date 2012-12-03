namespace NServiceBus.ActiveMQ
{
    public interface ITopicEvaluator
    {
        string GetTopicFromMessageType(string typeNames);
    }
}