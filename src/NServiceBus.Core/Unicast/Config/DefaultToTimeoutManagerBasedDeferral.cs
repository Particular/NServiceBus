namespace NServiceBus.Unicast.Config
{
    using NServiceBus.Config;
    using Settings;
    using Timeout;
    using Transports;

    public class DefaultToTimeoutManagerBasedDeferral : IFinalizeConfiguration
    {
        public void FinalizeConfiguration()
        {
            if (Configure.HasComponent<IDeferMessages>())
            {
                return;
            }

            Configure.Component<TimeoutManagerDeferrer>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.TimeoutManagerAddress, GetTimeoutManagerAddress());
        }

        static Address GetTimeoutManagerAddress()
        {
            var unicastConfig = Configure.GetConfigSection<UnicastBusConfig>();

            if (unicastConfig != null && !string.IsNullOrWhiteSpace(unicastConfig.TimeoutManagerAddress))
            {
                return Address.Parse(unicastConfig.TimeoutManagerAddress);
            }

            return SettingsHolder.Get<Address>("MasterNode.Address").SubScope("Timeouts");
        }
    }
}