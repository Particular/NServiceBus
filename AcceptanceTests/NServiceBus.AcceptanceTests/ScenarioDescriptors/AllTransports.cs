namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using NServiceBus.AcceptanceTesting.Support;

    public class AllTransports : ScenarioDescriptor
    {
        public AllTransports()
        {
            Add(Transports.Msmq);
            Add(Transports.ActiveMQ);
            Add(Transports.RabbitMQ);
            Add(Transports.SqlServer);
        }
    }

    public class AllDtcTransports : ScenarioDescriptor
    {
        public AllDtcTransports()
        {
            Add(Transports.Msmq);
            Add(Transports.ActiveMQ);
            Add(Transports.SqlServer);
        }
    }
}