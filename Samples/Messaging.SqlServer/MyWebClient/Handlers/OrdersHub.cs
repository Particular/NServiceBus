namespace MyWebClient.Handlers
{
    using System.Threading;
    using Microsoft.AspNet.SignalR;
    using MyMessages.Commands;
    using NServiceBus;

    public class OrdersHub : Hub
    {
        private static int orderNumber;

        public void CancelOrder(int orderNumber)
        {
            var command = new CancelOrder
            {
                ClientId = Context.ConnectionId,
                OrderNumber = orderNumber
            };

            MvcApplication.Bus.SetMessageHeader(command, "Debug", ((bool)Clients.Caller.debug).ToString());

            MvcApplication.Bus.Send(command);
        }

        public void PlaceOrder(string[] videoIds)
        {
            var command = new OrderCommand
            {
                ClientId = Context.ConnectionId,
                OrderNumber = Interlocked.Increment(ref orderNumber),
                VideoIds = videoIds
            };

            MvcApplication.Bus.SetMessageHeader(command, "Debug", ((bool)Clients.Caller.debug).ToString());

            MvcApplication.Bus.Send(command);
        }
    }
}