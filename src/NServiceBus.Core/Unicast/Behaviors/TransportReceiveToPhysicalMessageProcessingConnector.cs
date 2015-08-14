namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class TransportReceiveToPhysicalMessageProcessingConnector : StageConnector<TransportReceiveContext, PhysicalMessageProcessingStageBehavior.Context>
    {


        public override async Task Invoke(TransportReceiveContext context, Func<PhysicalMessageProcessingStageBehavior.Context, Task> next)
        {
            var physicalMessageContext = new PhysicalMessageProcessingStageBehavior.Context(context);

                await next(physicalMessageContext).ConfigureAwait(false);

            if (physicalMessageContext.AbortReceiveOperation)
            {
                throw new MessageProcessingAbortedException();
            }
        }
    }
}