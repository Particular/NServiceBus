using NServiceBus;

namespace OrderService
{
    public class InitialiseOrders : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.RegisterSingleton<OrderList>(new OrderList());
        }
    }
}
