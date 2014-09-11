namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Features;
    using Hosting.Profiles;
    using NServiceBus.Configuration.AdvanceExtensibility;

    class IntegrationProfileHandler : IHandleProfile<Integration>
    {
        public void ProfileActivated(BusConfiguration config)
        {
            config.GetSettings().EnableFeatureByDefault<InstallationSupport>();
        }

        public void ProfileActivated(Configure config)
        {
        }
    }
}