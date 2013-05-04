using Cashier;
using NServiceBus;
using NServiceBus.Features;
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
            Configure.Features.Disable<AutoSubscribe>();

            Configure.With()
                     .Log4Net()
                     .StructureMapBuilder(ObjectFactory.Container)

                     .AzureMessageQueue().JsonSerializer()
                     .Sagas().AzureSagaPersister()

                     .UseAzureTimeoutPersister()

                     .UnicastBus()
                     .LoadMessageHandlers()
                     .CreateBus()
                     .Start();
        }
    }
}
