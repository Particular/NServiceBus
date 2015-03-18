namespace NServiceBus.Features
{
    using NServiceBus.Config;
    using NServiceBus.Unicast.Queuing.Installers;

    /// <summary>
    /// Provides message forwarding capabilities
    /// </summary>
    public class ForwardReceivedMessages : Feature
    {
        internal ForwardReceivedMessages()
        {
            EnableByDefault();
            // Only enable if the configuration is defined in UnicastBus
            Prerequisite(config => GetConfiguredForwardMessageQueue(config) != null,"No forwarding address was defined in the unicastbus config");
        }

        /// <summary>
        /// Invoked if the feature is activated
        /// </summary>
        /// <param name="context">The feature context</param>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register<ForwardBehavior.Registration>();

            var forwardReceivedMessagesQueue = GetConfiguredForwardMessageQueue(context);
     
            context.Container.ConfigureComponent<ForwardReceivedMessagesToQueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.Enabled, true)
                .ConfigureProperty(t => t.Address, forwardReceivedMessagesQueue);

            context.Container.ConfigureComponent<ForwardBehavior>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.ForwardReceivedMessagesTo, forwardReceivedMessagesQueue);
        }

        string GetConfiguredForwardMessageQueue(FeatureConfigurationContext context)
        {
            var unicastBusConfig = context.Settings.GetConfigSection<UnicastBusConfig>();
            if (unicastBusConfig != null && !string.IsNullOrWhiteSpace(unicastBusConfig.ForwardReceivedMessagesTo))
            {
                return unicastBusConfig.ForwardReceivedMessagesTo;
            }
            return null;
        }
    }
}