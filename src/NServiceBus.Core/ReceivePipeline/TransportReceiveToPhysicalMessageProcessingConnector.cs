namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;

    class TransportReceiveToPhysicalMessageProcessingConnector : StageConnector<TransportReceiveContext, PhysicalMessageProcessingStageBehavior.Context>
    {

        public TransportReceiveToPhysicalMessageProcessingConnector(PipelineBase<BatchDispatchContext> batchDispatchPipeline)
        {
            this.batchDispatchPipeline = batchDispatchPipeline;
        }

        public override void Invoke(TransportReceiveContext context, Action<PhysicalMessageProcessingStageBehavior.Context> next)
        {
            var physicalMessageContext = new PhysicalMessageProcessingStageBehavior.Context(context);

            var batch = new DispatchBatch();

            physicalMessageContext.Set(batch);

            next(physicalMessageContext);

            physicalMessageContext.Remove<DispatchBatch>();

            if (physicalMessageContext.AbortReceiveOperation)
            {
                throw new MessageProcessingAbortedException();
            }

            if (batch.Operations.Any())
            {
                var batchDispatchContext = new BatchDispatchContext(batch.Operations, physicalMessageContext);

                batchDispatchPipeline.Invoke(batchDispatchContext);
            }
        }

        PipelineBase<BatchDispatchContext> batchDispatchPipeline;
    }

    class DispatchBatch
    {

        public IEnumerable<TransportOperation> Operations { get { return operations; } }

        public void Add(TransportOperation transportOperation)
        {
            operations.Add(transportOperation);
        }

        List<TransportOperation> operations = new List<TransportOperation>();
    }

    class BatchDispatchContext : BehaviorContext
    {
        public BatchDispatchContext(IEnumerable<TransportOperation> operations, BehaviorContext parentContext)
            : base(parentContext)
        {
        }
    }
}