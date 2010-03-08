using NServiceBus;

namespace MyServer
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server {}

    public class ServerInit : IWantCustomInitialization
    {
        public void Init()
        {
           Configure.Instance.BinarySerializer()
            .NHibernateFaultManagerWithSQLiteAndAutomaticSchemaGeneration();
        }
    }
}