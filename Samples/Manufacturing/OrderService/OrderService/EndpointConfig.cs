using NServiceBus;

namespace OrderService
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher {}

    public class TimeoutConfiguration:IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.RunTimeoutManager();
        }
    }
}