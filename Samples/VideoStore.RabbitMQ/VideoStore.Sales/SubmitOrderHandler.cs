namespace MyServer
{
    using System;
    using System.Diagnostics;
    using VideoStore.Common;
    using VideoStore.Messages.Commands;
    using VideoStore.Messages.Events;
    using VideoStore.Messages.RequestResponse;
    using NServiceBus;

    public class SubmitOrderHandler : IHandleMessages<SubmitOrder>
    {
        public IBus Bus { get; set; }

        public void Handle(SubmitOrder message)
        {
            if (DebugFlagMutator.Debug)
            {
                Debugger.Break();
            }

            Console.Out.WriteLine("We have received an order #{0} for [{1}] video(s).", message.OrderNumber,
                                  String.Join(", ", message.VideoIds));

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