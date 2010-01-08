using NServiceBus.Hosting.Profiles;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    internal class ProductionProfileHandler : IHandleProfile<Production>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance
                .NHibernateSagaPersister();

            Configure.Instance.Configurer.ConfigureComponent<Faults.InMemory.FaultManager>(ComponentCallModelEnum.Singleton);

            if (Config is AsA_Publisher)
                Configure.Instance.DBSubcriptionStorage();
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}