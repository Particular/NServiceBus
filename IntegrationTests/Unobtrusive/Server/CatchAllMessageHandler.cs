namespace Server
{
    using System;
    using NServiceBus;

    public class CatchAllMessageHandler : IHandleMessages<object>
    {
        public void Handle(object message)
        {
            Console.WriteLine("Catch all handler invoked");
        }
    }
}