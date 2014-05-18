namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Config;
    using Faults;
    using Hosting.Profiles;

    class IntegrationProfileHandler : IHandleProfile<Integration>
    {
        public void ProfileActivated(Configure config)
        {
            if (!config.Configurer.HasComponent<IManageMessageFailures>())
            {
                config.MessageForwardingInCaseOfFault();
            }
         
            WindowsInstallerRunner.RunInstallers = true;
        }
    }
}