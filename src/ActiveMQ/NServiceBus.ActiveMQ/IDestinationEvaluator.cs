namespace NServiceBus.Transport.ActiveMQ
{
    using Apache.NMS;

    public interface IDestinationEvaluator
    {
        IDestination GetDestination(ISession session, string destination, string prefix);
    }
}