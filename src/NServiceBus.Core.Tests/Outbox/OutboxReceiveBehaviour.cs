namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class OutboxReceiveBehaviour : IBehavior<PhysicalMessageContext>
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

            DispachOperationToTransport(outboxMessage.TransportOperations);

            OutboxStorage.SetAsDispatched(outboxMessage);
        }

        void DispachOperationToTransport(IEnumerable<TransportOperation> transportOperations)
        {
            foreach (var transportOperation in transportOperations)
            {
                //dispatch to transport
                PipelineFactory.InvokeSendPipeline(transportOperation.SendOptions, transportOperation.PhysicalMessage);
            }
        }

    }
}