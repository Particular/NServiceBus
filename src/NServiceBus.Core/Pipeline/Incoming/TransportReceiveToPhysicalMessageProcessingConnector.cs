namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Outbox;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;
    using TransportOperation = Transports.TransportOperation;

    class TransportReceiveToPhysicalMessageProcessingConnector : StageConnector<TransportReceiveContext, PhysicalMessageProcessingStageBehavior.Context>
    {
        public TransportReceiveToPhysicalMessageProcessingConnector(IPipelineBase<BatchDispatchContext> batchDispatchPipeline,IOutboxStorage outboxStorage)
        {
            this.batchDispatchPipeline = batchDispatchPipeline;
            this.outboxStorage = outboxStorage;
        }

        public async override Task Invoke(TransportReceiveContext context, Func<PhysicalMessageProcessingStageBehavior.Context, Task> next)
        {
            var physicalMessageContext = new PhysicalMessageProcessingStageBehavior.Context(context);
            var messageId = physicalMessageContext.GetPhysicalMessage().Id;

            var deduplicationEntry = await outboxStorage.Get(messageId, new OutboxStorageOptions(context)).ConfigureAwait(false);
            var pendingTransportOperations = new PendingTransportOperations();

            if (deduplicationEntry == null)
            {
                physicalMessageContext.Set(pendingTransportOperations);

                await next(physicalMessageContext).ConfigureAwait(false);

                if (physicalMessageContext.AbortReceiveOperation)
                {
                    throw new MessageProcessingAbortedException();
                }
                //todo: add pending ops
                await outboxStorage.Store(new OutboxMessage(messageId), new OutboxStorageOptions(context)).ConfigureAwait(false);
            }
            else
            {
                foreach (var operation in deduplicationEntry.TransportOperations)
                {
                    pendingTransportOperations.Add(new TransportOperation(new OutgoingMessage(operation.MessageId,operation.Headers,operation.Body), new DispatchOptions(null,null,DispatchConsistency.Isolated)));
                }
            }

            if (pendingTransportOperations.Operations.Any())
            {
                var batchDispatchContext = new BatchDispatchContext(pendingTransportOperations.Operations, physicalMessageContext);

                await batchDispatchPipeline.Invoke(batchDispatchContext).ConfigureAwait(false);
            }

            await outboxStorage.SetAsDispatched(messageId,new OutboxStorageOptions(context)).ConfigureAwait(false);
        }

        IPipelineBase<BatchDispatchContext> batchDispatchPipeline;
        IOutboxStorage outboxStorage;
    }
}