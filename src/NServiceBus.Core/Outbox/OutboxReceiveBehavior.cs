namespace NServiceBus.Outbox
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    class OutboxReceiveBehavior : IBehavior<PhysicalMessageContext>
    {
        public IOutboxStorage OutboxStorage { get; set; }

        public PipelineFactory PipelineFactory { get; set; }

        public void Invoke(PhysicalMessageContext context, Action next)
        {
            var messageId = context.PhysicalMessage.Id;

            var outboxMessage = OutboxStorage.Get(messageId);

            if (outboxMessage == null)
            {
                outboxMessage = new OutboxMessage { Id = messageId };

                context.Set(outboxMessage);

                //this runs the rest of the pipeline
                next();

                OutboxStorage.Store(outboxMessage);
            }

            if (outboxMessage.Dispatched)
            {
                return;
            }

            DispachOperationToTransport(outboxMessage);

            outboxMessage.Dispatched = true;
            OutboxStorage.SetAsDispatched(outboxMessage);
        }

        void DispachOperationToTransport(OutboxMessage outboxMessage)
        {
            outboxMessage.StartDispatching();
            foreach (var transportOperation in outboxMessage.TransportOperations)
            {
                //dispatch to transport
                PipelineFactory.InvokeSendPipeline(transportOperation.SendOptions, transportOperation.PhysicalMessage);
            }
        }

    }
}