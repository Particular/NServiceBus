namespace NServiceBus.Gateway.TestEndpoint
{
    using Config;

    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .Log4Net()
                .DefaultBuilder()
                .GatewayWithInMemoryPersistence()
                .MsmqTransport()
                .FileShareDataBus("../../../databus.storage")
                .UnicastBus();
        }
    }
}
