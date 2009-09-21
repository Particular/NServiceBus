using NServiceBus.ObjectBuilder;

namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Configures the infrastructure for the Lite profile
    /// </summary>
    public class LiteProfileHandler : IHandleProfile<Lite>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance.Configurer.ConfigureComponent<InMemorySagaPersister>(ComponentCallModelEnum.Singleton);

            if (Config is AsA_Publisher)
                Configure.Instance.Configurer.ConfigureComponent<InMemorySubscriptionStorage>(ComponentCallModelEnum.Singleton);
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}
