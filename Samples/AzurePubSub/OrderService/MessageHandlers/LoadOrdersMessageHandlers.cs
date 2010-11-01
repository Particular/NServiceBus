using System.Linq;
using MyMessages;
using NServiceBus;

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
            var reply = new LoadOrdersResponseMessage
                            {
                                Orders = orders.GetCurrentOrders().ToList()
                            };
            bus.Reply(reply);
        }
    }
}