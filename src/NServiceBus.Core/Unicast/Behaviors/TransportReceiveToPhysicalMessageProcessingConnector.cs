namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class TransportReceiveToPhysicalMessageProcessingConnector : StageConnector<TransportReceiveContext, PhysicalMessageProcessingStageBehavior.Context>
    {
        public override void Invoke(TransportReceiveContext context, Action<PhysicalMessageProcessingStageBehavior.Context> next)
        {
            var physicalMessageContext = new PhysicalMessageProcessingStageBehavior.Context(context);
            next(physicalMessageContext);
            if (!physicalMessageContext.MessageHandledSuccessfully)
            {
                throw new MessageProcessingAbortedException();
            }
        }
    }
}