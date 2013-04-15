namespace VideoStore.ECommerce.Handlers
{
    using System.Threading;
    using Microsoft.AspNet.SignalR;
    using VideoStore.Messages.Commands;
    using NServiceBus;

    public class OrdersHub : Hub
    {
        private readonly IBus bus;
        private static int orderNumber;

        public OrdersHub(IBus bus)
        {
            this.bus = bus;
        }

        public void CancelOrder(int orderNumber)
        {
            var command = new CancelOrder
            {
                ClientId = Context.ConnectionId,
                OrderNumber = orderNumber
            };

            bus.SetMessageHeader(command, "Debug", ((bool)Clients.Caller.debug).ToString());

            bus.Send(command);
        }

        public void PlaceOrder(string[] videoIds)
        {
            var command = new SubmitOrder
            {
                ClientId = Context.ConnectionId,
                OrderNumber = Interlocked.Increment(ref orderNumber),
                VideoIds = videoIds
            };

            bus.SetMessageHeader(command, "Debug", ((bool)Clients.Caller.debug).ToString());

            bus.Send(command);
        }
    }
}