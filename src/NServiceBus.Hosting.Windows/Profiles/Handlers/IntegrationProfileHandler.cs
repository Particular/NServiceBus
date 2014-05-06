namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Config;
    using Faults;
    using Hosting.Profiles;

    class IntegrationProfileHandler : IHandleProfile<Integration>
    {
        public void ProfileActivated()
        {
            if (!Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
            {
                Configure.Instance.MessageForwardingInCaseOfFault();
            }
         
            WindowsInstallerRunner.RunInstallers = true;
        }
    }
}