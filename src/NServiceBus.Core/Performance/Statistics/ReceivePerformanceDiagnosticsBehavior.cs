namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Janitor;
    using Pipeline;

    [SkipWeaving]
    class ReceivePerformanceDiagnosticsBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public ReceivePerformanceDiagnosticsBehavior(string transportAddress)
        {
            this.transportAddress = transportAddress;
        }

        public void Warmup()
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
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
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

        public void Cooldown()
        {
            messagesPulledFromQueueCounter?.Dispose();
            successRateCounter?.Dispose();
            failureRateCounter?.Dispose();
        }

        string transportAddress;
        IPerformanceCounterInstance messagesPulledFromQueueCounter;
        IPerformanceCounterInstance successRateCounter;
        IPerformanceCounterInstance failureRateCounter;
    }
}