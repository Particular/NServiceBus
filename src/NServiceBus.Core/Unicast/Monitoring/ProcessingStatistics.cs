namespace NServiceBus.Unicast.Monitoring
{
    using System;
    using UnitOfWork;

    /// <summary>
    /// Stores the start and end times for statistic purposes
    /// </summary>
    class ProcessingStatistics : IManageUnitsOfWork
    {
        CriticalTimeCalculator criticalTimeCounter;
        IBus bus;
        EstimatedTimeToSLABreachCalculator estimatedTimeToSLABreachCalculator;

        public ProcessingStatistics(CriticalTimeCalculator criticalTimeCounter, IBus bus, EstimatedTimeToSLABreachCalculator estimatedTimeToSLABreachCalculator)
        {
            this.criticalTimeCounter = criticalTimeCounter;
            this.bus = bus;
            this.estimatedTimeToSLABreachCalculator = estimatedTimeToSLABreachCalculator;
        }

        public void Begin()
        {
            bus.CurrentMessageContext.Headers[Headers.ProcessingStarted] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
        }

        public void End(Exception ex = null)
        {
            var now = DateTime.UtcNow;

            bus.CurrentMessageContext.Headers[Headers.ProcessingEnded] = DateTimeExtensions.ToWireFormattedString(now);

            string timeSent;
            if (bus.CurrentMessageContext.Headers.TryGetValue(Headers.TimeSent, out timeSent))
            {
                UpdateCounters(DateTimeExtensions.ToUtcDateTime(timeSent), DateTimeExtensions.ToUtcDateTime(bus.CurrentMessageContext.Headers[Headers.ProcessingStarted]), now);
            }
        }

        void UpdateCounters(DateTime timeSent, DateTime processingStarted, DateTime processingEnded)
        {
            if (criticalTimeCounter != null)
            {
                criticalTimeCounter.Update(timeSent, processingStarted,processingEnded);
            }


            if (estimatedTimeToSLABreachCalculator != null)
            {
                estimatedTimeToSLABreachCalculator.Update(timeSent, processingStarted, processingEnded);
            }
        }

    }
}