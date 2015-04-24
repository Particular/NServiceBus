namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using NServiceBus.Logging;
    using NServiceBus.Unicast.Transport.Monitoring;

    class ReceivePerformanceDiagnosticsBehavior : PhysicalMessageProcessingStageBehavior
    {
        static ILog Logger = LogManager.GetLogger<ReceivePerformanceDiagnostics>();

        bool enabled;
        PerformanceCounter counter;

        public override void OnStarting()
        {
            const string counterName = "# of msgs pulled from the input queue /sec";
            if (!PerformanceCounterHelper.TryToInstantiatePerformanceCounter(counterName, PipelineInfo.Name, out counter))
            {
                enabled = false;
            }

            Logger.DebugFormat("'{0}' counter initialized for '{1}'", counterName, PipelineInfo.Name);

            enabled = true;
        }

        public override void Invoke(Context context, Action next)
        {
            if (enabled)
            {
                counter.Increment();
            }
            next();
        }
    }
}