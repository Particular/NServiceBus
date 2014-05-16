namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Config;
    using Faults;
    using Hosting.Profiles;


    class LiteProfileHandler : IHandleProfile<Lite>, IWantTheEndpointConfig
    {
        public void ProfileActivated()
        {
            if (!Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
            {
                Configure.Instance.InMemoryFaultManagement();
            }

            WindowsInstallerRunner.RunInstallers = true;
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}