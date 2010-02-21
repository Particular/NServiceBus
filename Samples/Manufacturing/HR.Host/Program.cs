using System;
using NServiceBus;

namespace HR.Host
{
    class Program
    {
        static void Main()
        {
            var bus = NServiceBus.Configure.With()
                .Log4Net()
                .DefaultBuilder()
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false)
                    .LoadMessageHandlers()
                .CreateBus()
                .Start();

            Console.Read();
        }
    }
}
