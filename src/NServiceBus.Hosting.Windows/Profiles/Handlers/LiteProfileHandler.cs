namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Config;
    using Faults;
    using Hosting.Profiles;
    using Persistence.InMemory;

    class LiteProfileHandler : IHandleProfile<Lite>, IWantTheEndpointConfig
    {
        public void ProfileActivated()
        {
            InMemoryPersistence.UseAsDefault();

            if (!Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
            {
                Configure.Instance.InMemoryFaultManagement();
            }

            WindowsInstallerRunner.RunInstallers = true;
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}