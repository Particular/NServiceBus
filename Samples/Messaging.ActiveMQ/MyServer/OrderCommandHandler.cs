namespace MyServer
{
    using System;
    using System.Diagnostics;
    using Common;
    using MyMessages.Commands;
    using MyMessages.Events;
    using MyMessages.RequestResponse;
    using NServiceBus;

    public class OrderCommandHandler : IHandleMessages<OrderCommand>
    {
        public IBus Bus { get; set; }

        public void Handle(OrderCommand message)
        {
            if (DebugFlagMutator.Debug)
            {
                Debugger.Break();
            }

            Console.Out.WriteLine("We have received an order #{0} for [{1}] video(s).", message.OrderNumber,
                                  String.Join(", ", message.VideoIds));

            //send out a request (a event will be published when the response comes back)
            Bus.Send<CheckInventoryRequest>(r =>
                {
                    r.ClientId = message.ClientId;
                    r.OrderNumber = message.OrderNumber;
                    r.VideoIds = message.VideoIds;
                });

            //tell the client that we received the order
            Bus.Publish(Bus.CreateInstance<OrderPlaced>(o =>
                {
                    o.ClientId = message.ClientId;
                    o.OrderNumber = message.OrderNumber;
                    o.VideoIds = message.VideoIds;
                }));
        }
    }
}