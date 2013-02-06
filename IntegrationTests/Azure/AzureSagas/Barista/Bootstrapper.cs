using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Timeout.Hosting.Azure;
using StructureMap;

namespace Barista
{
    public class Bootstrapper
    {
        private Bootstrapper()
        {}

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
            Configure.With()
               .Log4Net()
               .StructureMapBuilder(ObjectFactory.Container)
               .AzureMessageQueue().JsonSerializer()
               .AzureSubcriptionStorage()
               .Sagas().AzureSagaPersister()
               .UseAzureTimeoutPersister().ListenOnAzureStorageQueues()
               .UnicastBus()
               .LoadMessageHandlers()
               .IsTransactional(true)
               .CreateBus()
               .Start();
        }
    }
}
