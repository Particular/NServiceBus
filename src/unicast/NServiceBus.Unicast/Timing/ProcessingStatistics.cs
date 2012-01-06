namespace NServiceBus.Unicast.Timing
{
    using System;
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

        public void Begin()
        {
            Bus.CurrentMessageContext.Headers["NServiceBus.ProcessingStarted"] = DateTime.UtcNow.ToWireFormattedString();
        }

        public void End(Exception ex = null)
        {
            Bus.CurrentMessageContext.Headers["NServiceBus.ProcessingEnded"] = DateTime.UtcNow.ToWireFormattedString();
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<ProcessingStatistics>(DependencyLifecycle.InstancePerCall);
        }
    }
}