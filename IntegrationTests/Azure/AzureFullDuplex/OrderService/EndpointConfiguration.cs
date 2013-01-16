using MyMessages;
using NServiceBus;

namespace OrderService
{
    public class EndpointConfiguration : IConfigureThisEndpoint, AsA_Worker, IWantCustomInitialization
    {
        public void Init()
        {
           Configure.Instance.DefiningMessagesAs(m => typeof (IDefineMessages).IsAssignableFrom(m));

        }
    }
}