namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    class DisabledPublishingTerminator : PipelineTerminator<IOutgoingPublishContext>
    {
        protected override Task Terminate(IOutgoingPublishContext context, CancellationToken token)
        {
            throw new InvalidOperationException("Publishing has been explicitly disabled on this endpoint. Remove 'transportSettings.DisablePublishing()' to enable publishing again.");
        }
    }
}