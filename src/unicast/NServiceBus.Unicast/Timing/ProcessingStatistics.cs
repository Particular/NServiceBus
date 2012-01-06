namespace NServiceBus.Unicast.Timing
{
    using System;
    using System.Diagnostics;
    using Config;
    using UnitOfWork;

    /// <summary>
    /// Stores the start and endtimes for statistic purposes
    /// </summary>
    public class ProcessingStatistics:IManageUnitsOfWork,INeedInitialization
    {
        /// <summary>
        /// Needs the bus to set the headers
        /// </summary>
        public IBus Bus { get; set; }

        /// <summary>
        /// Performance counter for critcal time. 
        /// </summary>
        public CriticalTimePerformanceCounter CriticalTimeCounter { get; set; }

        public void Begin()
        {
            Bus.CurrentMessageContext.Headers[Headers.ProcessingStarted] = DateTime.UtcNow.ToWireFormattedString();
        }

        public void End(Exception ex = null)
        {
            var now = DateTime.UtcNow;

            Bus.CurrentMessageContext.Headers[Headers.ProcessingEnded] = now.ToWireFormattedString();

            if (CriticalTimeCounter != null && Bus.CurrentMessageContext.Headers.ContainsKey(Headers.TimeSent))
                CriticalTimeCounter.Update(Bus.CurrentMessageContext.Headers[Headers.TimeSent].ToUtcDateTime(), now);
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<ProcessingStatistics>(DependencyLifecycle.InstancePerCall);
        }
    }
}