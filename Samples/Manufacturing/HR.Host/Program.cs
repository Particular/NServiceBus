using System;
using Common.Logging;
using NServiceBus;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Config;
using HR.MessageHandlers;

namespace HR.Host
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("HR Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            try
            {
                NServiceBus.Config.Configure.With(builder)
                    .InterfaceToXMLSerializer()
                    .MsmqTransport()
                        .IsTransactional(true)
                        .PurgeOnStartup(false)
                    .UnicastBus()
                        .ImpersonateSender(false)
                        .SetMessageHandlersFromAssembliesInOrder(
                            typeof(GridInterceptingMessageHandler).Assembly
                            , typeof(RequestOrderAuthorizationMessageHandler).Assembly
                        );

                IBus bServer = builder.Build<IBus>();
                bServer.Start();
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
