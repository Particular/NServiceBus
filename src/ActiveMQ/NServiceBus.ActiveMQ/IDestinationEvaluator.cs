namespace NServiceBus.Transports.ActiveMQ
{
    using Apache.NMS;

    public interface IDestinationEvaluator
    {
        IDestination GetDestination(ISession session, string destination, string prefix);
    }
}