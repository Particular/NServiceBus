using NServiceBus;
using NServiceBus.Timeout.Hosting.Azure;

namespace OrderService
{
    public class EndpointInitialisation : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.UseAzureTimeoutPersister()
                .ListenOnAzureServiceBusQueues();

            Configure.Instance.Configurer.RegisterSingleton<OrderList>(new OrderList());
        }
    }
}
