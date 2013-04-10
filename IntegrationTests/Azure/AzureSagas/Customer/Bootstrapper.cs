using Cashier;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Timeout.Hosting.Azure;
using StructureMap;

namespace Customer
{
    public class Bootstrapper
    {
        private Bootstrapper()
        {
        }

        public static void Bootstrap()
        {
            BootstrapStructureMap();
            BootstrapNServiceBus();
        }

        private static void BootstrapStructureMap()
        {
            ObjectFactory.Initialize(x => x.AddRegistry(new CustomerRegistry()));
        }

        private static void BootstrapNServiceBus()
        {
            Configure.Transactions.Enable();

            Configure.With()
                     .Log4Net()
                     .StructureMapBuilder(ObjectFactory.Container)

                     .AzureMessageQueue().JsonSerializer()
                     .Sagas().AzureSagaPersister()

                     .UseAzureTimeoutPersister()

                     .UnicastBus()
                     .DoNotAutoSubscribe()
                     .LoadMessageHandlers()
                     .CreateBus()
                     .Start();
        }
    }
}
