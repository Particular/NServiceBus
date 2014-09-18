namespace NServiceBus.Features
{
    using Config;
    using Timeout;

    /// <summary>
    /// Adds the ability to defer messages using a timeoutmanager
    /// </summary>
    public class TimeoutManagerBasedDeferral:Feature
    {
        internal TimeoutManagerBasedDeferral()
        {
            
        }
        /// <summary>
        /// Invoked if the feature is activated
        /// </summary>
        /// <param name="context">The feature context</param>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<TimeoutManagerDeferrer>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.TimeoutManagerAddress, GetTimeoutManagerAddress(context));
        }

        static Address GetTimeoutManagerAddress(FeatureConfigurationContext context)
        {
            var unicastConfig = context.Settings.GetConfigSection<UnicastBusConfig>();

            if (unicastConfig != null && !string.IsNullOrWhiteSpace(unicastConfig.TimeoutManagerAddress))
            {
                return Address.Parse(unicastConfig.TimeoutManagerAddress);
            }

            return context.Settings.Get<Address>("MasterNode.Address").SubScope("Timeouts");
        }
    }
}