using System;
using Common.Logging;
using Messages;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;

namespace Subscriber1
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            Configure.With(builder).SagasAndMessageHandlersIn(typeof(EventMessageHandler).Assembly);

            NServiceBus.Serializers.Configure.BinarySerializer.With(builder);
            //NServiceBus.Serializers.Configure.XmlSerializer.With(builder);

            new ConfigMsmqTransport(builder)
                .IsTransactional(false)
                .PurgeOnStartup(false);

            new ConfigUnicastBus(builder)
                .ImpersonateSender(false)
                .SetMessageHandlersFromAssembliesInOrder(
                    typeof(EventMessageHandler).Assembly
                );

            IBus bus = builder.Build<IBus>();

            bus.Start();

            bus.Subscribe(typeof(EventMessage));

            Console.WriteLine("Listening for events. To exit, press 'q' and then 'Enter'.");
            while (Console.ReadLine().ToLower() != "q")
            {
            }
        }
    }
}
