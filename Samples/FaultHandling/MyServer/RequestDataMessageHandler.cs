using System;
using MyMessages;
using NServiceBus;

namespace MyServer
{
    public class RequestDataMessageHandler : IHandleMessages<RequestDataMessage>
    {
        private static readonly Random _random = new Random();

        public IBus Bus { get; set; }

        public void Handle(RequestDataMessage message)
        {
            if (message.Fault)
            {
                Console.WriteLine("Fault message: throwing exception");
                throw new Exception();
            }
            Bus.Reply(new DataResponseMessage { Fault = Fault() });
            Console.WriteLine("Message processed");
        }

        private static bool Fault()
        {
            return _random.Next(2) == 0;
        }
    }
}
