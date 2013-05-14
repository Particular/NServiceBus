using MyMessages;
using NServiceBus;

namespace OrderService
{
    public class DefineMessages : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            Configure.Instance.DefiningMessagesAs(t => typeof(IDefineMessages).IsAssignableFrom(t) && t != typeof(IDefineMessages) );
        }
    }
}