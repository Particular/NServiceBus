namespace Client
{
    using System;
    using Events;
    using NServiceBus;

    public class MyEventHandler:IHandleMessages<MyEvent>
    {
        public void Handle(MyEvent message)
        {
            Console.WriteLine("MyEvent received from server with id:" + message.EventId);
        }
    }
}