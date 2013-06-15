using System;
using NServiceBus;

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
            Configure.Transactions.Enable();

            Configure.With()
               .DefaultBuilder()
               .AzureMessageQueue()
               .AzureDataBus()
               .UnicastBus()
                    .LoadMessageHandlers()
               .CreateBus()
               .Start();
        }
    }
}
