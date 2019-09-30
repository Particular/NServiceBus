namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class DisabledPublishingConnector : StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext>
    {
        public override Task Invoke(IOutgoingPublishContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            throw new InvalidOperationException("Publishing has been explicitly disabled on this endpoint. Remove 'transportSettings.DisablePublishing()' to enable publishing again.");
        }
    }
}