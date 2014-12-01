using NServiceBus.Faults;
using NServiceBus.Hosting.Profiles;


namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Persistence.InMemory;

    internal class LiteProfileHandler : IHandleProfile<Lite>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            InMemoryPersistence.UseAsDefault();

            Configure.Instance.AsMasterNode();

            if (!Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
                Configure.Instance.InMemoryFaultManagement();

            WindowsInstallerRunner.RunInstallers = true;
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}