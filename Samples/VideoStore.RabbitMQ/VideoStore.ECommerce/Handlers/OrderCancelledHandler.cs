namespace VideoStore.ECommerce.Handlers
{
    using Microsoft.AspNet.SignalR;
    using VideoStore.Messages.Events;
    using NServiceBus;

    public class OrderCancelledHandler : IHandleMessages<OrderCancelled>
    {
        public void Handle(OrderCancelled message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<OrdersHub>();

            context.Clients.Client(message.ClientId).orderCancelled(new
                {
                    message.OrderNumber,
                });
        }
    }
}