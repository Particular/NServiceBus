using MyMessages;
using NServiceBus;

namespace OrderWebSite.MessageHandlers
{
    public class OrderUpdatedEventHandler : IHandleMessages<OrderUpdatedEvent>
    {
        public void Handle(OrderUpdatedEvent message)
        {
            var order = message.UpdatedOrder;

            lock (Global.Orders)
            {
                if (Global.Orders.Contains(order))
                    Global.Orders.Remove(order);

                Global.Orders.Add(order);
            }
        }
    }
}