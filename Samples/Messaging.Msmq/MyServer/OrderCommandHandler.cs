namespace MyServer
{
    using System;
    using MyMessages.Commands;
    using MyMessages.RequestResponse;
    using NServiceBus;

    public class OrderCommandHandler : IHandleMessages<OrderCommand>
    {
        public IBus Bus { get; set; }

        public void Handle(OrderCommand message)
        {
            Console.Out.WriteLine("We have received an order #{0} for [{1}] video(s).", message.OrderNumber, String.Join(", ", message.VideoIds));
            
            //send out a request (a event will be published when the response comes back)
            Bus.Send<CheckInventoryRequest>(r =>
                {
                    r.OrderNumber = message.OrderNumber;
                    r.VideoIds = message.VideoIds;
                });

            //tell the client that we accepted the command
            Bus.Return(OrderStatus.Ok);
        }
    }
}