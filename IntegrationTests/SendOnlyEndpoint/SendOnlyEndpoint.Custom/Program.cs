using System;
using NServiceBus;

namespace SendOnlyEndpoint.Custom
{
    public class Program
    {
        static void Main()
        {
            var configuration = new BusConfiguration();
            configuration.UsePersistence<InMemoryPersistence>();

            using (var bus = Bus.CreateSendOnly(configuration))
            {
                bus.Send("SendOnlyDestination@someserver",new TestMessage());
            }

            Console.WriteLine("Message sent to remote endpoint, you can verify this by looking at the outgoing queues in you msmq MMC-snapin"); 
            Console.WriteLine("Press any key to exit");

            Console.ReadKey();
        }
    }

    public class TestMessage : IMessage{}
}
