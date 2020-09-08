namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    class SendOnlySubscribeTerminator : PipelineTerminator<ISubscribeContext>
    {
        protected override Task Terminate(ISubscribeContext context, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Send-only endpoints cannot subscribe to events. Remove the 'endpointConfiguration.SendOnly()' configuration to enable this endpoint to subscribe to events.");
        }
    }
}