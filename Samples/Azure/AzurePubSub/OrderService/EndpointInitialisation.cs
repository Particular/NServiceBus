using NServiceBus;

namespace OrderService
{
    public class EndpointInitialisation : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.RegisterSingleton<OrderList>(new OrderList());
        }
    }
}
