using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Host.Profiles.Handlers
{
    internal class LiteProfileHandler : IHandleProfile<Lite>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance.InMemorySagaPersister();

            if (Config is AsA_Publisher)
                Configure.Instance.InMemorySubscriptionStorage();
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}