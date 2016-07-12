namespace NServiceBus.Features
{
    using Transport;

    /// <summary>
    /// Provides message forwarding capabilities.
    /// </summary>
    public class ForwardReceivedMessages : Feature
    {
        internal ForwardReceivedMessages()
        {
            EnableByDefault();

            Prerequisite(config => config.Settings.HasSetting(ConfigureForwarding.SettingsKey), "No forwarding address was defined in the UnicastBus config");
        }

        /// <summary>
        /// Invoked if the feature is activated.
        /// </summary>
        /// <param name="context">The feature context.</param>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var forwardReceivedMessagesQueue = context.Settings.Get<string>(ConfigureForwarding.SettingsKey);

            context.Settings.Get<QueueBindings>().BindSending(forwardReceivedMessagesQueue);

            context.Pipeline.Register("InvokeForwardingPipeline", new InvokeForwardingPipelineBehavior(forwardReceivedMessagesQueue), "Execute the forwarding pipeline");

            context.Pipeline.Register(new ForwardingToRoutingConnector(), "Makes sure that forwarded messages gets dispatched to the transport");
        }
    }
}