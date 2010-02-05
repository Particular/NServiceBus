using System;
using Common.Logging;
using NServiceBus;
using NServiceBus.Grid.MessageHandlers;
using HR.MessageHandlers;

namespace HR.Host
{
    class Program
    {
        static void Main()
        {
            var bus = NServiceBus.Configure.With()
                .Log4Net()
                .SpringBuilder()
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
