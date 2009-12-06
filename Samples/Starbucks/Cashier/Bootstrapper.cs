using NServiceBus;
using StructureMap;

namespace Cashier
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
            ObjectFactory.Initialize(x => x.AddRegistry(new CashierRegistry()));
        }

        private static void BootstrapNServiceBus()
        {
            Configure.With()
                .StructureMapBuilder(ObjectFactory.Container)
                .MsmqSubscriptionStorage()
                .XmlSerializer()
                // For sagas
                .Sagas()
                .NHibernateSagaPersisterWithSQLiteAndAutomaticSchemaGeneration()
                // End
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false)
                    .LoadMessageHandlers()
                .CreateBus()
                .Start();
        }
    }
}
