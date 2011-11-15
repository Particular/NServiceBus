using NServiceBus;
using NServiceBus.ObjectBuilder;
using NServiceBus.Sagas.Impl;
using NServiceBus.Timeout.Hosting.Azure;
using StructureMap;
using Timeout.MessageHandlers;
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
               .TimeoutManager()
               .UnicastBus()
               .LoadMessageHandlers(First<TimeoutMessageHandler>.Then<SagaMessageHandler>())
               .IsTransactional(true)
               .CreateBus()
               .Start();
        }
    }
}
