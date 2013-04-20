using NServiceBus;
using NServiceBus.Config;
using StructureMap;

namespace Barista
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
            ObjectFactory.Initialize(x => x.AddRegistry(new BaristaRegistry()));
        }

        private static void BootstrapNServiceBus()
        {
            Configure.Transactions.Enable();

            Configure.With()
                     .Log4Net()
                     .StructureMapBuilder(ObjectFactory.Container)
                     .AzureMessageQueue().JsonSerializer()
                     .AzureSubcriptionStorage()
                     .Sagas().AzureSagaPersister()
                     .UseAzureTimeoutPersister()
                     .UnicastBus()
                     .LoadMessageHandlers()
                     .CreateBus()
                     .Start();
        }
    }
}
