using NServiceBus;
using StructureMap;

namespace Barista
{
    using NServiceBus.Features;

    public class Bootstrapper
    {
        private Bootstrapper()
        { }

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
            Configure.Features.Enable<Sagas>();
            Configure.Serialization.Json();

            Configure.With()
                .Log4Net()
                .StructureMapBuilder(ObjectFactory.Container)
                .AzureMessageQueue()
                .UseNHibernateSubscriptionPersister()
                .UseNHibernateSagaPersister()
                .UseNHibernateTimeoutPersister()
                .UnicastBus()
                .LoadMessageHandlers()
                .CreateBus()
                .Start();
        }
    }
}
