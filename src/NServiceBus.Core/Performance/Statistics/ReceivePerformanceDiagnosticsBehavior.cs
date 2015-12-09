namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Janitor;
    using Performance.Counters;
    using Pipeline;

    [SkipWeaving]
    class ReceivePerformanceDiagnosticsBehavior : Behavior<IncomingPhysicalMessageContext>
    {
   
        public override Task Warmup()
        {
            messagesPulledFromQueueCounter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter("# of msgs pulled from the input queue /sec", PipelineInfo.TransportAddress);
            successRateCounter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter("# of msgs successfully processed / sec", PipelineInfo.TransportAddress);
            failureRateCounter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter("# of msgs failures / sec", PipelineInfo.TransportAddress);
         
            return base.Warmup();
        }

        public override async Task Invoke(IncomingPhysicalMessageContext context, Func<Task> next)
        {
            messagesPulledFromQueueCounter.Increment();

            try
            {
                await next().ConfigureAwait(false);
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
            messagesPulledFromQueueCounter?.Dispose();
            successRateCounter?.Dispose();
            failureRateCounter?.Dispose();

            return base.Cooldown();
        }

        IPerformanceCounterInstance messagesPulledFromQueueCounter;
        IPerformanceCounterInstance successRateCounter;
        IPerformanceCounterInstance failureRateCounter;


       
    }
}