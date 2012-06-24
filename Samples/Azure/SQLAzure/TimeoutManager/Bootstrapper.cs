using NServiceBus;
using NServiceBus.Timeout.Hosting.Azure;
using NServiceBus.Timeout.Hosting.Windows;
using StructureMap;
using Configure = NServiceBus.Configure;

namespace TimeoutManager
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
            ObjectFactory.Initialize(x => x.AddRegistry(new TimeoutRegistry()));
        }

        private static void BootstrapNServiceBus()
        {
            Configure.With()
               .Log4Net()
               .StructureMapBuilder(ObjectFactory.Container)
                    .AzureMessageQueue().JsonSerializer()
               .DefineEndpointName("notused")
               .RunTimeoutManager()
                    .UseAzureTimeoutPersister()
                    .ListenOnAzureStorageQueues()
               .UnicastBus()
                   .LoadMessageHandlers()
                   .IsTransactional(true)
               .CreateBus()
               .Start();
        }
    }
}
