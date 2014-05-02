namespace NServiceBus.Outbox
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Pipeline.Contexts;

    class OutboxReceiveBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public IOutboxStorage OutboxStorage { get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }
        
        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            var messageId = context.PhysicalMessage.Id;
            OutboxMessage outboxMessage;

            if (!OutboxStorage.TryGet(messageId, out outboxMessage))
            {
                outboxMessage = new OutboxMessage(messageId);

                context.Set(outboxMessage);

                //this runs the rest of the pipeline
                next();

                OutboxStorage.Store(messageId, outboxMessage.TransportOperations);
            }

            if (outboxMessage.Dispatched)
            {
                return;
            }

            DispatchOperationToTransport(outboxMessage.TransportOperations);

            context.Set("Outbox_StartDispatching", true);
            OutboxStorage.SetAsDispatched(messageId);
        }

        void DispatchOperationToTransport(IEnumerable<TransportOperation> operations)
        {
            foreach (var transportOperation in operations)
            {
                //dispatch to transport
                PipelineExecutor.InvokeSendPipeline(transportOperation.SendOptions, transportOperation.Message);
            }
        }
    }
}