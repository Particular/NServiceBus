namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Janitor;
    using Performance.Counters;
    using Pipeline;

    [SkipWeaving]
    class ReceivePerformanceDiagnosticsBehavior : Behavior<PhysicalMessageProcessingContext>
    {
        public ReceivePerformanceDiagnosticsBehavior(string transportAddress)
        {
            this.transportAddress = transportAddress;
        }

        public override Task Warmup()
        {
            messagesPulledFromQueueCounter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter(
                "# of msgs pulled from the input queue /sec", 
                transportAddress);

            successRateCounter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter(
                "# of msgs successfully processed / sec", 
                transportAddress);

            failureRateCounter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter(
                "# of msgs failures / sec", 
                transportAddress);
         
            return base.Warmup();
        }

        public override async Task Invoke(PhysicalMessageProcessingContext context, Func<Task> next)
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

        string transportAddress;
    }
}