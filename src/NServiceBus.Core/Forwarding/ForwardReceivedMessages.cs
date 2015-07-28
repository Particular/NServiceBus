namespace NServiceBus.Features
{
    using NServiceBus.Config;
    using NServiceBus.Forwarding;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// Provides message forwarding capabilities.
    /// </summary>
    public class ForwardReceivedMessages : Feature
    {
        internal ForwardReceivedMessages()
        {
            EnableByDefault();
            // Only enable if the configuration is defined in UnicastBus
            Prerequisite(config => GetConfiguredForwardMessageQueue(config) != null, "No forwarding address was defined in the UnicastBus config");
        }

        /// <summary>
        /// Invoked if the feature is activated.
        /// </summary>
        /// <param name="context">The feature context.</param>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var forwardReceivedMessagesQueue = GetConfiguredForwardMessageQueue(context);

            context.Settings.Get<QueueBindings>().BindSending(forwardReceivedMessagesQueue);

            context.Pipeline.Register<InvokeForwardingPipelineBehavior.Registration>();
            context.Pipeline.RegisterConnector<ForwardingToDispatchConnector>("Makes sure that forwarded messages gets dispatched to the transport");

            context.Container.ConfigureComponent(b =>
            {
                var pipelinesCollection = context.Settings.Get<PipelineConfiguration>();
                var pipeline = new PipelineBase<ForwardingContext>(b, context.Settings, pipelinesCollection.MainPipeline);

                return new InvokeForwardingPipelineBehavior(pipeline, forwardReceivedMessagesQueue);
            }, DependencyLifecycle.InstancePerCall);
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