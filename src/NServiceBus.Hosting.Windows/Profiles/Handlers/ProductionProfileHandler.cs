namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Faults;
    using Hosting.Profiles;

    class ProductionProfileHandler : IHandleProfile<Production>
    {
        void IHandleProfile.ProfileActivated()
        {
            if (!Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
            {
                Configure.Instance.MessageForwardingInCaseOfFault();
            }
        }
    }
}