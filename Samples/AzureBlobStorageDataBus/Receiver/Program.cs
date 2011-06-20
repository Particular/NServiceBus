using System;
using NServiceBus;
using NServiceBus.Config;

namespace Receiver
{
    class Program
    {
        static void Main(string[] args)
        {
            BootstrapNServiceBus();

            Console.WriteLine("Press enter to stop receiving");
            Console.ReadLine();

        }

        private static void BootstrapNServiceBus()
        {
            Configure.With()
               .DefaultBuilder()
               .AzureConfigurationSource()
               .AzureMessageQueue()
                    .BinarySerializer()
               .AzureDataBus()
               .UnicastBus()
                    .LoadMessageHandlers()
                    .IsTransactional(true)
               .CreateBus()
               .Start();
        }
    }
}
