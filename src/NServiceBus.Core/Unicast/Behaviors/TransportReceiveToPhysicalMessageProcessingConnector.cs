namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Janitor;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    [SkipWeaving]
    class TransportReceiveToPhysicalMessageProcessingConnector : StageConnector<TransportReceiveContext, PhysicalMessageProcessingStageBehavior.Context>
    {
        IPerformanceCounterInstance successRateCounter;
        IPerformanceCounterInstance failureRateCounter;

        public override Task Warmup()
        {
            successRateCounter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter("# of msgs successfully processed / sec", PipelineInfo.PublicAddress);
            failureRateCounter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter("# of msgs failures / sec", PipelineInfo.PublicAddress);
            return base.Warmup();
        }

        public override Task Cooldown()
        {
            if (successRateCounter != null)
            {
                successRateCounter.Dispose();
            }
            if (failureRateCounter != null)
            {
                failureRateCounter.Dispose();
            }
            return base.Cooldown();
        }

        public override void Invoke(TransportReceiveContext context, Action<PhysicalMessageProcessingStageBehavior.Context> next)
        {
            var physicalMessageContext = new PhysicalMessageProcessingStageBehavior.Context(context);
            try
            {
                next(physicalMessageContext);
                if (physicalMessageContext.AbortReceiveOperation)
                {
                    throw new MessageProcessingAbortedException();
                }
                successRateCounter.Increment();

            }
            catch (Exception)
            {
                failureRateCounter.Increment();
                throw;
            }
        }
    }
}