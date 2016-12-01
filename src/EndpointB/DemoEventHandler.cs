using System;
using System.Threading.Tasks;
using Contracts.Events;
using NServiceBus;

namespace EndpointB
{
    class DemoEventHandler : IHandleMessages<DemoEvent>
    {
        public Task Handle(DemoEvent message, IMessageHandlerContext context)
        {
            Console.WriteLine($"Received {nameof(DemoEvent)} {message.EventId}");
            return Task.CompletedTask;
        }
    }
}
