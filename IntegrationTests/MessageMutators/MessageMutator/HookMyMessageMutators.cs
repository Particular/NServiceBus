using NServiceBus;

namespace MessageMutators
{
    public class HookMyMessageMutators : INeedInitialization
    {
        public void Init(Configure config)
        {
            config.Configurer.ConfigureComponent<ValidationMessageMutator>(
                DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<TransportMessageCompressionMutator>(
                DependencyLifecycle.InstancePerCall);
        }
    }
}
