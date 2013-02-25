namespace MyRequestResponseEndpoint
{
    using System;
    using System.Diagnostics;
    using Common;
    using MyMessages.RequestResponse;
    using NServiceBus;

    public class CheckInventoryHandler : IHandleMessages<CheckInventoryRequest>
    {
        public IBus Bus { get; set; }

        public void Handle(CheckInventoryRequest message)
        {
            if (DebugFlagMutator.Debug)
            {
                Debugger.Break();
            }

            Console.Out.WriteLine("It looks like we have [{0}] video(s) in stock.", String.Join(", ", message.VideoIds));

            Bus.Reply(new InventoryResponse
                {
                    OrderNumber = message.OrderNumber,
                    VideoIds = message.VideoIds,
                    ClientId = message.ClientId
                });
        }
    }
}