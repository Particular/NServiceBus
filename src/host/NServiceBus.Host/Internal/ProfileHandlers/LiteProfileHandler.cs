using NServiceBus.ObjectBuilder;

namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Configures the infrastructure for the Lite profile
    /// </summary>
    public class LiteProfileHandler : IConfigureTheBusForProfile<Lite>
    {
        void IConfigureTheBus.Configure(IConfigureThisEndpoint specifier)
        {
            NServiceBus.Configure.With()
                .SpringBuilder()
                .XmlSerializer()
                .Sagas();
                
            Configure.Instance.Configurer.ConfigureComponent<InMemorySagaPersister>(ComponentCallModelEnum.Singleton);

            if (specifier is AsA_Publisher)
                Configure.Instance.Configurer.ConfigureComponent<InMemorySubscriptionStorage>(ComponentCallModelEnum.Singleton);
        }
    }
}
