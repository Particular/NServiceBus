using NServiceBus;

namespace MessageMutators
{
    public class HookMyMessageMutators : INeedInitialization
    {
        public void Customize(BusConfiguration configuration)
        {
            configuration.RegisterComponents(c =>
            {
                c.ConfigureComponent<ValidationMessageMutator>(
                    DependencyLifecycle.InstancePerCall);
                c.ConfigureComponent<TransportMessageCompressionMutator>(
                DependencyLifecycle.InstancePerCall);
            });
        }
    }
}
