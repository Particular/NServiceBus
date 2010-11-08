using MyMessages;
using NServiceBus;

namespace OrderWebSite.MessageHandlers
{
    public class OrderUpdatedEventHandler : IHandleMessages<OrderUpdatedEvent>
    {
        public void Handle(OrderUpdatedEvent message)
        {
            var order = message.UpdatedOrder;

            lock (WebRole.Orders)
            {
                if (WebRole.Orders.Contains(order))
                    WebRole.Orders.Remove(order);

                WebRole.Orders.Add(order);
            }
        }
    }
}