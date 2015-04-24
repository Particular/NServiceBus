namespace NServiceBus
{
    using System;
    using Janitor;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    [SkipWeaving]
    class TransportReceiveToPhysicalMessageProcessingConnector : StageConnector<TransportReceiveContext, PhysicalMessageProcessingStageBehavior.Context>, IDisposable
    {
        IPerformanceCounterInstance successRateCounter;
        IPerformanceCounterInstance failureRateCounter;

        public override void OnStarting()
        {
            successRateCounter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter("# of msgs successfully processed / sec", PipelineInfo.PublicAddress);
            failureRateCounter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter("# of msgs failures / sec", PipelineInfo.PublicAddress);
        }

        public override void Invoke(TransportReceiveContext context, Action<PhysicalMessageProcessingStageBehavior.Context> next)
        {
            var physicalMessageContext = new PhysicalMessageProcessingStageBehavior.Context(context);
            try
            {
                next(physicalMessageContext);
                if (!physicalMessageContext.MessageHandledSuccessfully)
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

        public void Dispose()
        {
            if (successRateCounter != null)
            {
                successRateCounter.Dispose();
            }
            if (failureRateCounter != null)
            {
                failureRateCounter.Dispose();
            }
        }
    }
}