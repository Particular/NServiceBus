namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Janitor;

    [SkipWeaving]
    class ReceivePerformanceDiagnosticsBehavior : PhysicalMessageProcessingStageBehavior
    {
        IPerformanceCounterInstance counter;

        public override Task Warmup()
        {
            counter = PerformanceCounterHelper.TryToInstantiatePerformanceCounter("# of msgs pulled from the input queue /sec", PipelineInfo.PublicAddress);
            return base.Cooldown();
        }

        public override Task Cooldown()
        {
            if (counter != null)
            {
                counter.Dispose();
            }
            return base.Cooldown();
        }

        public override void Invoke(Context context, Action next)
        {
            counter.Increment();
            next();
        }
    }
}