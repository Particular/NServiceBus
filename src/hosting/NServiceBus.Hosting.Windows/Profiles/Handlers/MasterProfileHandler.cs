using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    class MasterProfileHandler : IHandleProfile<Master>
    {
        public void ProfileActivated()
        {
            Configure.Instance.AsMasterNode()
                .UseDistributor()
                .Gateway()
                .UseTimeoutManagerWithInMemoryPersistence();
        }
    }
}
