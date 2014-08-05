namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Faults;
    using Features;
    using Hosting.Profiles;


    class LiteProfileHandler : IHandleProfile<Lite>
    {
        public void ProfileActivated(ConfigurationBuilder config)
        {
        }

        public void ProfileActivated(Configure config)
        {
            if (!config.Configurer.HasComponent<IManageMessageFailures>())
            {
                //config.InMemoryFaultManagement();
            }

            config.Settings.EnableFeatureByDefault<InstallationSupport>();
        }
    }
}