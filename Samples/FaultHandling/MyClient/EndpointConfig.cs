using NServiceBus;
using NServiceBus.Faults;
using NServiceBus.Faults.NHibernate;

namespace MyClient
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client {}

    public class ClientInit : IWantCustomInitialization
    {
        public void Init()
        {
           Configure.Instance.BinarySerializer()
            .NHibernateFaultManagerWithSQLiteAndAutomaticSchemaGeneration();         
        }
    }
}