using System;
using NServiceBus;

namespace HR.Host
{
    class Program
    {
        static void Main()
        {
            Configure.Transactions.Enable();

            Configure.With()
                .Log4Net()
                .DefaultBuilder()
                .XmlSerializer()
                .UseTransport<Msmq>()
                    .PurgeOnStartup(false)
                    .InMemorySubscriptionStorage()
                .UnicastBus()
                    .RunHandlersUnderIncomingPrincipal(false)
                    .LoadMessageHandlers()
                .CreateBus()
                .Start(() => Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install());

            Console.Read();
        }
    }
}
