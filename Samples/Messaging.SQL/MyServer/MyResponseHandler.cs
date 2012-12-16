namespace MyServer
{
    using System;
    using MyMessages;
    using MyMessages.Events;
    using MyMessages.RequestResponse;
    using NServiceBus;

    public class MyResponseHandler : IHandleMessages<MyResponse>
    {
        public IBus Bus { get; set; }

        public void Handle(MyResponse message)
        {
            Console.Out.WriteLine("MyResponse message received with data: " + message.ResponseData);

            Bus.Publish<MyEvent>();
        }
    }
}