using Microsoft.ServiceHosting.ServiceRuntime;
using MyMessages;
using NServiceBus;

namespace OrderWebSite.MessageHandlers
{
    public class LoadOrdersResponseMessageHandler : IHandleMessages<LoadOrdersResponseMessage>
    {
        public void Handle(LoadOrdersResponseMessage message)
        {
            RoleManager.WriteToLog("Information", "LoadOrdersResponseMessage received");

            lock (Global.Orders)
                Global.Orders = message.Orders;
        }
    }
}