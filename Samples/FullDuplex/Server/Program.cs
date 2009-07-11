using System;
using Common.Logging;
using NServiceBus;

namespace Server
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");

            try
            {
                var bus = NServiceBus.Configure.With()
                    .SpringBuilder()
                    .XmlSerializer("http://www.UdiDahan.com")
                    .MsmqTransport()
                        .IsTransactional(true)
                        .PurgeOnStartup(false)
                    .UnicastBus()
                        .ImpersonateSender(false)
                        .LoadMessageHandlers()
                    .CreateBus();

                bus.Start();

                Console.Read();
            }
            catch (Exception e)
            {
                LogManager.GetLogger("hello").Fatal("Exiting", e);
                Console.Read();
            }
        }
    }
}
