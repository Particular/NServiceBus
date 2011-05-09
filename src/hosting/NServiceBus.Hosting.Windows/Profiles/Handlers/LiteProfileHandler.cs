using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    internal class LiteProfileHandler : IHandleProfile<Lite>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance.InMemorySagaPersister();

            Configure.Instance.InMemoryFaultManagement();

            if (Config is AsA_Publisher)
                Configure.Instance.InMemorySubscriptionStorage();

            Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install();
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}