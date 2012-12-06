namespace MySubscriber
{
    using System;
    using MyMessages;
    using NServiceBus;

    public class MyEventHandler:IHandleMessages<MyEvent>
    {
        public void Handle(MyEvent message)
        {
            Console.Out.WriteLine("MyEvent received from MyServer");
        }
    }
}