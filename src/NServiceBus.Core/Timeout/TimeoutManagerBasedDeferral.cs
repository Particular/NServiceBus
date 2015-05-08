namespace NServiceBus.Features
{
    using Config;
    using NServiceBus.Transports;
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
            var timeoutManagerAddress = GetTimeoutManagerAddress(context);

            context.Container.ConfigureComponent(b =>new TimeoutManagerDeferrer(b.Build<ISendMessages>(), timeoutManagerAddress), 
                DependencyLifecycle.InstancePerCall);
        }

        static string GetTimeoutManagerAddress(FeatureConfigurationContext context)
        {
            var unicastConfig = context.Settings.GetConfigSection<UnicastBusConfig>();

            if (unicastConfig != null && !string.IsNullOrWhiteSpace(unicastConfig.TimeoutManagerAddress))
            {
                return unicastConfig.TimeoutManagerAddress;
            }
            var selectedTransportDefinition = context.Settings.Get<TransportDefinition>();
            return selectedTransportDefinition.GetSubScope(context.Settings.Get<string>("MasterNode.Address"), "Timeouts");
        }
    }
}