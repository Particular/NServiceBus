using System;
using System.Threading;
using MyMessages;
using NServiceBus;
using Order = MyMessages.Order;

namespace OrderService.MessageHandlers
{
    public class OrderMessageHandler : IHandleMessages<OrderMessage>
    {
        public IBus Bus { get; set; }
        
        public OrderList Orders { get; set; }
     
        public void Handle(OrderMessage message)
        {
            //simulate processing
            Thread.Sleep(4000);

            //simulate business logic
            var order = new Order
            {
                Id = message.Id,
                Quantity = message.Quantity,
                Status = message.Quantity < 100 ? OrderStatus.Approved : OrderStatus.AwaitingApproval
            };

            //store the order in "the database"
            Orders.AddOrder(order);

            //publish update
            Bus.Publish<OrderUpdatedEvent>(x => x.UpdatedOrder = order);
        }
    }
}