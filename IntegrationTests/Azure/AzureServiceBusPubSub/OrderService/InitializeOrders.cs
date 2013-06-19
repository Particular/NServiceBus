using NServiceBus;

namespace OrderService
{
    public class InitializeOrders : INeedInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.RegisterSingleton<OrderList>(new OrderList());
        }
    }
}
