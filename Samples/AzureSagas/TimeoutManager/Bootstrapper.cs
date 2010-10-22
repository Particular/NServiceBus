using NServiceBus;
using NServiceBus.Config;
using StructureMap;

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
               
               .AzureConfigurationSource()
               .AzureMessageQueue().XmlSerializer()
               //.AzureSubcriptionStorage()
               //.Sagas().AzureSagaPersister()

               .UnicastBus()
               .LoadMessageHandlers()
               .IsTransactional(true)
               .CreateBus()
               .Start();
        }
    }
}
