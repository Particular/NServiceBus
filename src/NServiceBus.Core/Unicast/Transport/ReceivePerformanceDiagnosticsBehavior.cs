namespace NServiceBus
{
    using System;
    using Janitor;

    [SkipWeaving]
    class ReceivePerformanceDiagnosticsBehavior : PhysicalMessageProcessingStageBehavior, IDisposable
    {
        IPerformanceCounterInstance counter;

        public override void OnStarting()
        {
            counter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter("# of msgs pulled from the input queue /sec", PipelineInfo.PublicAddress);
        }

        public override void Invoke(Context context, Action next)
        {
            counter.Increment();
            next();
        }

        public void Dispose()
        {
            if (counter != null)
            {
                counter.Dispose();
            }
        }
    }
}