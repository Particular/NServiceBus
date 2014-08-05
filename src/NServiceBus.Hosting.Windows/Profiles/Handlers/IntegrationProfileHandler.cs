namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Faults;
    using Features;
    using Hosting.Profiles;

    class IntegrationProfileHandler : IHandleProfile<Integration>
    {
        public void ProfileActivated(ConfigurationBuilder config)
        {

        }

        public void ProfileActivated(Configure config)
        {
            if (!config.Configurer.HasComponent<IManageMessageFailures>())
            {
                config.MessageForwardingInCaseOfFault();
            }

            config.Settings.EnableFeatureByDefault<InstallationSupport>();
        }
    }
}