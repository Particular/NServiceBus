namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Config;
    using Faults;
    using Hosting.Profiles;


    class LiteProfileHandler : IHandleProfile<Lite>, IWantTheEndpointConfig
    {
        public void ProfileActivated(Configure config)
        {
            if (!config.Configurer.HasComponent<IManageMessageFailures>())
            {
                config.InMemoryFaultManagement();
            }

            WindowsInstallerRunner.RunInstallers = true;
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}