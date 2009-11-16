using MyMessages;
using NServiceBus;

namespace OrderWebSite.MessageHandlers
{
    public class LoadOrdersResponseMessageHandler : IHandleMessages<LoadOrdersResponseMessage>
    {
        public void Handle(LoadOrdersResponseMessage message)
        {
            lock (Global.Orders)
                Global.Orders = message.Orders;
        }
    }
}