using MyMessages;
using NServiceBus;

namespace OrderService
{
    public class DoNotAutoSubscribe : IWantCustomInitialization
    {
        public void Init()
        {
           // Configure.Instance.UnicastBus().DoNotAutoSubscribe();
        }
    }
}