using System;
using MyMessages;
using NServiceBus;

namespace MyClient
{
    class DataResponseMessageHandler : IHandleMessages<DataResponseMessage>
    {
        public void Handle(DataResponseMessage message)
        {
            if (message.Fault)
            {
                Console.WriteLine("Fault message: throwing exception");
                throw new Exception();
            }
            Console.WriteLine("Message processed");
        }
    }
}
