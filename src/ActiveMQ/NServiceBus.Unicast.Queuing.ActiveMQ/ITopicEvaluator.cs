namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    public interface ITopicEvaluator
    {
        string GetTopicFromMessageType(string typeNames);
    }
}