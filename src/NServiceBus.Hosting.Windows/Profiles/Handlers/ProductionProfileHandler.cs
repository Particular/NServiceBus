namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Faults;
    using Hosting.Profiles;

    class ProductionProfileHandler : IHandleProfile<Production>
    {
        public void ProfileActivated(Configure config)
        {
            if (!config.Configurer.HasComponent<IManageMessageFailures>())
            {
                config.MessageForwardingInCaseOfFault();
            }
        }
    }
}