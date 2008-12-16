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
            LogManager.GetLogger("hello").Debug("HR Started.");

            try
            {
                var bus = NServiceBus.Configure.With()
                    .SpringBuilder()
                    .XmlSerializer()
                    .MsmqTransport()
                        .IsTransactional(true)
                        .PurgeOnStartup(false)
                    .UnicastBus()
                        .ImpersonateSender(false)
                        .SetMessageHandlersFromAssembliesInOrder(
                            typeof(GridInterceptingMessageHandler).Assembly
                            , typeof(RequestOrderAuthorizationMessageHandler).Assembly
                        )
                    .CreateBus()
                    .Start();
            }
            catch (Exception e)
            {
                LogManager.GetLogger("hello").Fatal("Exiting", e);
                Console.Read();
            }

            Console.Read();
        }
    }
}
