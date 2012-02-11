namespace NServiceBus.Unicast.Monitoring
{
    using System;
    using NServiceBus.Config;
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


        /// <summary>
        /// Counter that displays the estimated time left to a SLA breach
        /// </summary>
        public EstimatedTimeToSLABreachCalculator EstimatedTimeToSLABreachCalculator { get; set; }

        public void Begin()
        {
            Bus.CurrentMessageContext.Headers[Headers.ProcessingStarted] = DateTime.UtcNow.ToWireFormattedString();
        }

        public void End(Exception ex = null)
        {
            var now = DateTime.UtcNow;

            Bus.CurrentMessageContext.Headers[Headers.ProcessingEnded] = now.ToWireFormattedString();

            if (Bus.CurrentMessageContext.Headers.ContainsKey(Headers.TimeSent))
                UpdateCounters(Bus.CurrentMessageContext.Headers[Headers.TimeSent].ToUtcDateTime(), now);
            
                
        }

        void UpdateCounters(DateTime timeSent, DateTime timeProcessed)
        {
            if(CriticalTimeCounter != null)
                CriticalTimeCounter.Update(timeSent,timeProcessed);


            if (EstimatedTimeToSLABreachCalculator != null)
                EstimatedTimeToSLABreachCalculator.Update(timeSent, timeProcessed);
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<ProcessingStatistics>(DependencyLifecycle.InstancePerCall);
        }
    }
}