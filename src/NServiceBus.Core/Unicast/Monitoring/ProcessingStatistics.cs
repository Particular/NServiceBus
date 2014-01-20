namespace NServiceBus.Unicast.Monitoring
{
    using System;
    using UnitOfWork;

    /// <summary>
    /// Stores the start and end times for statistic purposes
    /// </summary>
    public class ProcessingStatistics : IManageUnitsOfWork, INeedInitialization
    {
        /// <summary>
        /// Needs the bus to set the headers
        /// </summary>
        public IBus Bus { get; set; }

        /// <summary>
        /// Performance counter for critical time. 
        /// </summary>
        public CriticalTimeCalculator CriticalTimeCounter { get; set; }


        /// <summary>
        /// Counter that displays the estimated time left to a SLA breach
        /// </summary>
        public EstimatedTimeToSLABreachCalculator EstimatedTimeToSLABreachCalculator { get; set; }

        public void Begin()
        {
            Bus.CurrentMessageContext.Headers[Headers.ProcessingStarted] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
        }

        public void End(Exception ex = null)
        {
            var now = DateTime.UtcNow;

            Bus.CurrentMessageContext.Headers[Headers.ProcessingEnded] = DateTimeExtensions.ToWireFormattedString(now);

            string timeSent;
            if (Bus.CurrentMessageContext.Headers.TryGetValue(Headers.TimeSent, out timeSent))
            {
                UpdateCounters(DateTimeExtensions.ToUtcDateTime(timeSent), DateTimeExtensions.ToUtcDateTime(Bus.CurrentMessageContext.Headers[Headers.ProcessingStarted]), now);
            }
        }

        void UpdateCounters(DateTime timeSent, DateTime processingStarted, DateTime processingEnded)
        {
            if (CriticalTimeCounter != null)
            {
                CriticalTimeCounter.Update(timeSent, processingStarted,processingEnded);
            }


            if (EstimatedTimeToSLABreachCalculator != null)
            {
                EstimatedTimeToSLABreachCalculator.Update(timeSent, processingStarted, processingEnded);
            }
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<ProcessingStatistics>(DependencyLifecycle.InstancePerUnitOfWork);
        }
    }
}