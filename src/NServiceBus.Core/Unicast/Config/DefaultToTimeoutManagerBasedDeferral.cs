namespace NServiceBus.Unicast.Config
{
    using NServiceBus.Config;
    using Timeout;
    using Transports;

    public class DefaultToTimeoutManagerBasedDeferral : IFinalizeConfiguration
    {
        public void FinalizeConfiguration(Configure config)
        {
            if (config.Configurer.HasComponent<IDeferMessages>())
            {
                return;
            }

            config.Configurer.ConfigureComponent<TimeoutManagerDeferrer>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.TimeoutManagerAddress, GetTimeoutManagerAddress(config));
        }

        static Address GetTimeoutManagerAddress(Configure config)
        {
            var unicastConfig = config.GetConfigSection<UnicastBusConfig>();

            if (unicastConfig != null && !string.IsNullOrWhiteSpace(unicastConfig.TimeoutManagerAddress))
            {
                return Address.Parse(unicastConfig.TimeoutManagerAddress);
            }

            return config.Settings.Get<Address>("MasterNode.Address").SubScope("Timeouts");
        }
    }
}