namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class ReceivePerformanceDiagnosticsBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public ReceivePerformanceDiagnosticsBehavior(string queueName)
        {
            this.queueName = queueName;
        }

        public void Warmup()
        {
            messagesPulledFromQueueCounter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter(
                "# of msgs pulled from the input queue /sec",
                queueName);
            successRateCounter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter(
                "# of msgs successfully processed / sec",
                queueName);
            failureRateCounter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter(
                "# of msgs failures / sec",
                queueName);
        }

        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            messagesPulledFromQueueCounter.Increment();

            try
            {
                await next(context).ConfigureAwait(false);
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

        IPerformanceCounterInstance failureRateCounter;
        IPerformanceCounterInstance messagesPulledFromQueueCounter;
        IPerformanceCounterInstance successRateCounter;

        string queueName;
    }
}