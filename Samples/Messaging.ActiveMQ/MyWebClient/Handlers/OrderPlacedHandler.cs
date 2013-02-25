namespace MyWebClient.Handlers
{
    using Microsoft.AspNet.SignalR;
    using MyMessages.Events;
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