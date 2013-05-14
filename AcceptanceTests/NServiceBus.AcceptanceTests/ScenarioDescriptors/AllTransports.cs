namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using AcceptanceTesting.Support;

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

    public class AllDtcTransports : AllTransports
    {
        public AllDtcTransports()
        {   
            Remove(Transports.RabbitMQ);
        }
    }

    public class AllBrokerTransports : AllTransports
    {
        public AllBrokerTransports()
        {
            Remove(Transports.Msmq);
        }
    }

    public class AllTransportsWithCentralizedPubSubSupport : AllTransports
    {
        public AllTransportsWithCentralizedPubSubSupport()
        {
            Remove(Transports.Msmq);
            Remove(Transports.SqlServer);
        }
    }

    public class AllTransportsWithMessageDrivenPubSub : AllTransports
    {
        public AllTransportsWithMessageDrivenPubSub()
        {
            Remove(Transports.ActiveMQ);
            Remove(Transports.RabbitMQ);
        }
    }
}