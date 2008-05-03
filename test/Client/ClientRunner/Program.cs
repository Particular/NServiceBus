using System;
using Messages;
using NServiceBus;
using Common.Logging;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;
using NServiceBus.Config;
using Client;

namespace ClientRunner
{
    class Program
    {
        static void Main()
        {
            try
            {
                LogManager.GetLogger("hello").Debug("Started.");
                ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

                string sendWF = System.Configuration.ConfigurationManager.AppSettings["SendWF"];

                Configure.With(builder).SagasAndMessageHandlersIn(typeof (EventMessageHandler).Assembly);

                new ConfigMsmqTransport(builder)
                    .IsTransactional(false)
                    .PurgeOnStartup(false)
                    .UseXmlSerialization(false);

                new ConfigUnicastBus(builder)
                    .ImpersonateSender(false)
                    .SetMessageHandlersFromAssembliesInOrder(
                        typeof(EventMessageHandler).Assembly
                    );

                IBus bus = builder.Build<IBus>();

                bus.Start();

                bus.Subscribe(typeof(Event));

                int toSend = 1;
                Console.WriteLine("Press 'Enter' to send a message. To exit, press 'q' and then 'Enter'.");
                while (Console.ReadLine().ToLower() != "q")
                {
                    Command m = new Command();
                    m.i = toSend;

                    toSend++;

                    bus.Send(m);

                    Console.WriteLine("{0}.{1}  Sent command: {2}", DateTime.Now.Second, DateTime.Now.Millisecond, m.i);

                    if (sendWF == "true")
                    {
                        bus.Send(new PriceQuoteRequest());

                        Console.WriteLine("{0}.{1}  Sent WF: {2}", DateTime.Now.Second, DateTime.Now.Millisecond, m.i);
                    }
                }

                bus.Unsubscribe(typeof(Event));
            }
            catch (Exception e)
            {
                LogManager.GetLogger("hello").Error("Fatal", e);
                Console.Read();
            }
        }
    }
}
