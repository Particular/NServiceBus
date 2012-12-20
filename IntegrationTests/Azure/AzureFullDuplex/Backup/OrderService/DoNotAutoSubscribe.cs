using MyMessages;
using NServiceBus;

namespace OrderService
{
    public class DoNotAutoSubscribe : IWantCustomInitialization
    {
        public void Init()
        {
           // Configure.Instance.UnicastBus().DoNotAutoSubscribe();
            Configure.Instance.DefiningMessagesAs(m => typeof (IDefineMessages).IsAssignableFrom(m));
        }
    }
}