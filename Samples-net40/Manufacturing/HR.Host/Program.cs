using System;
using log4net;
using NServiceBus;

namespace HR.Host
{
    class Program
    {
        static void Main()
        {
            try
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
            }
            catch (Exception e)
            {
                LogManager.GetLogger("HR").Fatal("Exiting", e);
                Console.Read();
            }

            Console.Read();
        }
    }
}
