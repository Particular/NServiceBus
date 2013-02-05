namespace MyWebClient.Handlers
{
    using System.Linq;
    using Microsoft.AspNet.SignalR;
    using MyMessages.Events;
    using NServiceBus;

    public class OrderIsReadyHandler :  IHandleMessages<OrderIsReady>
    {
        public void Handle(OrderIsReady message)
        {
            var context = GlobalHost.ConnectionManager.GetConnectionContext<OrderConnection>();
            
            context.Connection.Broadcast(new
                {
                    message.OrderNumber, 
                    VideoUrls = message.VideoUrls.Select(pair => new {Id = pair.Key, Url = pair.Value}).ToArray()
                });
        }


    }
}