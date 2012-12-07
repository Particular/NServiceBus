namespace Server
{
    using System;
    using Messages;
    using NServiceBus;

    public class RequestMessageHandler : IHandleMessages<Request>
    {
        public IBus Bus { get; set; }

        public void Handle(Request message)
        {
            Console.WriteLine("Request received with id:" + message.RequestId);

            Bus.Reply(new Response
                          {
                              ResponseId = message.RequestId
                          });
        }
    }
}