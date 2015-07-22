namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Janitor;
    using NServiceBus.Performance.Counters;

    [SkipWeaving]
    class ReceivePerformanceDiagnosticsBehavior : PhysicalMessageProcessingStageBehavior
    {
   
        public override Task Warmup()
        {
            messagesPulledFromQueueCounter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter("# of msgs pulled from the input queue /sec", PipelineInfo.PublicAddress);
            successRateCounter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter("# of msgs successfully processed / sec", PipelineInfo.PublicAddress);
            failureRateCounter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter("# of msgs failures / sec", PipelineInfo.PublicAddress);
         
            return base.Cooldown();
        }

        public override void Invoke(Context context, Action next)
        {
            messagesPulledFromQueueCounter.Increment();

            try
            {
                next();
            }
            catch (Exception)
            {
                failureRateCounter.Increment();
                throw;
            }

            successRateCounter.Increment();
        }

        public override Task Cooldown()
        {
            if (messagesPulledFromQueueCounter != null)
            {
                messagesPulledFromQueueCounter.Dispose();
            }

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

        IPerformanceCounterInstance messagesPulledFromQueueCounter;
        IPerformanceCounterInstance successRateCounter;
        IPerformanceCounterInstance failureRateCounter;


       
    }
}