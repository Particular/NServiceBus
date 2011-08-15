using NServiceBus;
using NServiceBus.ObjectBuilder;
using NServiceBus.Sagas.Impl;
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
            var configure = Configure.With()
                .Log4Net()
                .StructureMapBuilder(ObjectFactory.Container);

            configure.Configurer.ConfigureComponent<Timeout.MessageHandlers.TimeoutManager>(DependencyLifecycle.SingleInstance);
            configure.Configurer.ConfigureComponent<TimeoutPersister>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(tp => tp.ConnectionString, "UseDevelopmentStorage=true");
            configure.Configurer.ConfigureComponent<Timeout.MessageHandlers.Bootstrapper>(
                DependencyLifecycle.SingleInstance);

            configure
               .AzureMessageQueue().JsonSerializer()
               .UnicastBus()
               .LoadMessageHandlers(First<TimeoutMessageHandler>.Then<SagaMessageHandler>())
               .IsTransactional(true)
               .CreateBus()
               .Start();

            var bootstrapper = configure.Builder.Build<Timeout.MessageHandlers.Bootstrapper>();
            bootstrapper.Run();
        }
    }
}
