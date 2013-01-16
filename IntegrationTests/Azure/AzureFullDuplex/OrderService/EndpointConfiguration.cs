using MyMessages;
using NServiceBus;

namespace OrderService
{
    public class EndpointConfiguration : IConfigureThisEndpoint, AsA_Worker{}

    public class DefineMessages : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            Configure.Instance.DefiningMessagesAs(m => typeof(IDefineMessages).IsAssignableFrom(m));
        }
    }
}