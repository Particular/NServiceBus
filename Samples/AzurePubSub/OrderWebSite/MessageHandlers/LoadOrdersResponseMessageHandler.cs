using MyMessages;
using NServiceBus;

namespace OrderWebSite.MessageHandlers
{
    public class LoadOrdersResponseMessageHandler : IHandleMessages<LoadOrdersResponseMessage>
    {
        public void Handle(LoadOrdersResponseMessage message)
        {
            lock (WebRole.Orders)
                WebRole.Orders = message.Orders;
        }
    }
}