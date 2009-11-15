using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.ServiceHosting.ServiceRuntime;
using MyMessages;
using NServiceBus;
using Order=MyMessages.Order;

namespace OrderService.MessageHandlers
{
    public class LoadOrdersMessageHandlers : IHandleMessages<LoadOrdersMessage>
    {
        private readonly IBus bus;
        private readonly OrderList orders;

        public LoadOrdersMessageHandlers(IBus bus,OrderList orders)
        {
            this.bus = bus;
            this.orders = orders;
        }

        public void Handle(LoadOrdersMessage message)
        {
            RoleManager.WriteToLog("Information", "LoadOrdersMessage received");

            var reply = new LoadOrdersResponseMessage
                            {
                                Orders = orders.GetCurrentOrders().ToList()
                            };
            bus.Reply(reply);
        }
    }
}