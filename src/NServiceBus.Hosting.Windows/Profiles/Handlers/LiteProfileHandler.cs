namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Features;
    using Hosting.Profiles;
    using NServiceBus.Configuration.AdvanceExtensibility;


    class LiteProfileHandler : IHandleProfile<Lite>
    {
        public void ProfileActivated(ConfigurationBuilder config)
        {
            config.GetSettings().EnableFeatureByDefault<InstallationSupport>();
            //if (!config.Configurer.HasComponent<IManageMessageFailures>()) //TODO: Not sure how to handle this yet
            //{
                config.DiscardFailedMessagesInsteadOfSendingToErrorQueue();
            //}
        }

        public void ProfileActivated(Configure config)
        {
            
        }
    }
}