namespace NServiceBus.Audit
{
    class InitMessageAuditer : INeedInitialization
    {
        public void Init(Configure config)
        {
            // Configure the <see cref="MessageAuditer"/> component. If the feature is enabled then
            // the appropriate audit queue parameters will be set. Both the unicast bus
            // and the gateway sender will call this component to forward the messages
            // to the audit queue. This component will forward it only if the auditing is enabled.
            config.Configurer.ConfigureComponent<MessageAuditer>(DependencyLifecycle.SingleInstance);
        }
    }
}
