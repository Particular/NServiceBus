namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Config;
    using Faults;
    using Hosting.Profiles;


    class LiteProfileHandler : IHandleProfile<Lite>
    {
        public void ProfileActivated(Configure config)
        {
            if (!config.Configurer.HasComponent<IManageMessageFailures>())
            {
                config.InMemoryFaultManagement();
            }

            WindowsInstallerRunner.RunInstallers = true;
        }
    }
}