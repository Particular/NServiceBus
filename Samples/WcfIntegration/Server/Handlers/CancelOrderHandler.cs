using System;
using Messages;
using NServiceBus;

namespace Server.Handlers
{
    public class CancelOrderHandler : IHandleMessages<CancelOrder>
    {
        private readonly IBus bus;

        public CancelOrderHandler(IBus bus)
        {
            this.bus = bus;
        }

        public void Handle(CancelOrder message)
        {
            Console.WriteLine("======================================================================");

            if (message.OrderId % 2 == 0)
                bus.Return((int) ErrorCodes.Fail);
            else
                bus.Return((int) ErrorCodes.None);
        }
    }
}