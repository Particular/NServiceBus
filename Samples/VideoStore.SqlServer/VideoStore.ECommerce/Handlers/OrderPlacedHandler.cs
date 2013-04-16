namespace VideoStore.ECommerce.Handlers
{
    using Microsoft.AspNet.SignalR;
    using VideoStore.Messages.Events;
    using NServiceBus;

    public class OrderPlacedHandler : IHandleMessages<OrderPlaced>
    {
        public void Handle(OrderPlaced message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<OrdersHub>();

            context.Clients.Client(message.ClientId).orderReceived(new
                {
                    message.OrderNumber,
                    message.VideoIds
                });
        }
    }
}