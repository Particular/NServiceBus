namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class SendOnlyUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
    {
        protected override Task Terminate(IUnsubscribeContext context)
        {
            throw new InvalidOperationException("Send-only endpoints cannot unsubscribe to events. Remove the 'endpointConfiguration.SendOnly()' configuration to enable this endpoint to unsubscribe to events.");
        }
    }
}