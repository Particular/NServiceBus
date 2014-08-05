namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Features;
    using Hosting.Profiles;

    class IntegrationProfileHandler : IHandleProfile<Integration>
    {
        public void ProfileActivated(ConfigurationBuilder config)
        {
        }

        public void ProfileActivated(Configure config)
        {
            config.Settings.EnableFeatureByDefault<InstallationSupport>();
        }
    }
}