using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Contracts.Commands;
using Contracts.Events;
using NServiceBus;

namespace EndpointB
{
    class DemoCommandHandler : IHandleMessages<DemoCommand>
    {
        public async Task Handle(DemoCommand message, IMessageHandlerContext context)
        {
            Console.WriteLine($"Received {nameof(DemoCommand)} {message.CommandId}");
            await context.Publish(new DemoCommandReceived {ReceivedCommandId = message.CommandId});
        }
    }
}
