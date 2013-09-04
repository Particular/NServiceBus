namespace NServiceBus.Audit
{
    class ConfigureAudit : INeedInitialization
    {
        public void Init()
        {
            // Configure the <see cref="MessageAuditer"/> component. If the feature is enabled then
            // the appropriate audit queue parameters will be set. Both the unicast bus
            // and the gateway sender will call this component to forward the messages
            // to the audit queue. This component will forward it only if the auditing is enabled.
            Configure.Component<MessageAuditer>(DependencyLifecycle.SingleInstance);
        }
    }
}
