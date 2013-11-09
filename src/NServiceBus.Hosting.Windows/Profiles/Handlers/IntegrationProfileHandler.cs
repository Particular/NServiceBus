namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Faults;
    using Hosting.Profiles;

    class IntegrationProfileHandler : IHandleProfile<Integration>
    {
        void IHandleProfile.ProfileActivated()
        {
            if (!Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
            {
                Configure.Instance.MessageForwardingInCaseOfFault();
            }
         
            WindowsInstallerRunner.RunInstallers = true;
        }
    }
}