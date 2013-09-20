namespace Client
{
    using System;
    using Events;
    using NServiceBus;

    public class MyEventHandler : IHandleMessages<IMyEvent>
    {
        public void Handle(IMyEvent message)
        {
            Console.WriteLine("IMyEvent received from server with id:" + message.EventId);
        }
    }
}