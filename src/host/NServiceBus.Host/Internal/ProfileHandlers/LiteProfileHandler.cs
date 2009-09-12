using System.Collections.Specialized;
using Common.Logging;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Configures the infrastructure for the Lite profile
    /// </summary>
    public class LiteProfileHandler : IHandleProfileConfiguration<Lite>
    {
        void IHandleProfileConfiguration.Init(IConfigureThisEndpoint specifier) { }

        void IHandleProfileConfiguration.ConfigureSagas(Configure busConfiguration)
        {
            Configure.TypeConfigurer.ConfigureComponent<InMemorySagaPersister>(ComponentCallModelEnum.Singleton);
        }

        void IHandleProfileConfiguration.ConfigureSubscriptionStorage(Configure busConfiguration)
        {
            Configure.TypeConfigurer.ConfigureComponent<InMemorySubscriptionStorage>(
                ComponentCallModelEnum.Singleton);
        }
    }
}
