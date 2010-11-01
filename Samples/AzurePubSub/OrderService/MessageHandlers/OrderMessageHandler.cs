using System.Threading;
using MyMessages;
using NServiceBus;
using Order=MyMessages.Order;

namespace OrderService.MessageHandlers
{
    public class OrderMessageHandler : IHandleMessages<OrderMessage>
    {
        private readonly IBus bus;
        private readonly OrderList orders;

        public OrderMessageHandler(IBus bus, OrderList orders)
        {
            this.bus = bus;
            this.orders = orders;
        }

        public void Handle(OrderMessage message)
        {
            var order = new Order
                            {
                                Id = message.Id, 
                                Quantity = message.Quantity
                            };
            //simulate processing
            Thread.Sleep(4000);
            
            //simlute business logic
            order.Status = message.Quantity < 100 ? OrderStatus.Approved : OrderStatus.AwaitingApproval;

            orders.AddOrder(order);
            
            //publish update
            var orderUpdatedEvent = bus.CreateInstance<OrderUpdatedEvent>(x=>x.UpdatedOrder = order);
            bus.Publish(orderUpdatedEvent);
        }
    }
}