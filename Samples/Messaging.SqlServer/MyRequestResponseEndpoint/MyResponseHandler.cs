namespace MyRequestResponseEndpoint
{
    using System;
    using MyMessages.RequestResponse;
    using NServiceBus;

    public class MyResponseHandler : IHandleMessages<MyRequest>
    {
        public IBus Bus { get; set; }

        public void Handle(MyRequest message)
        {
            Console.Out.WriteLine("MyRequest message received with data: " + message.RequestData);

            Bus.Reply(new MyResponse { ResponseData = "This is a response from the MyRequestResponseEndpoint" });
        }
    }
}