namespace NServiceBus.Unicast.Monitoring
{
    using System;
    using System.Diagnostics;
    using NServiceBus.Config;

    /// <summary>
    /// Initializes the peformcecounters if they are enabled
    /// </summary>
    public class PerformanceCounterInitializer : IWantToRunWhenConfigurationIsComplete
    {
        public void Run()
        {
            if (!Configure.Instance.PerformanceCountersEnabled())
                return;

            SetupCriticalTimePerformanceCounter();

            SetupSLABreachCounter();
        }

        static void SetupCriticalTimePerformanceCounter()
        {
            var criticalTimeCounter = new CriticalTimePerformanceCounter();

            PerformanceCounter counter;
            try
            {
                counter = new PerformanceCounter(CategoryName, "Critical Time", Configure.EndpointName, false);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("NServiceBus performance counter for Critical Time not set up correctly. Please run the NServiceBus infrastructure installers to rectify this problem.",e);
            }
            criticalTimeCounter.Initialize(counter);

            Configure.Instance.Configurer.RegisterSingleton<CriticalTimePerformanceCounter>(criticalTimeCounter);
        }

        static void SetupSLABreachCounter()
        {
            var endpointSla = Configure.Instance.EndpointSLA();

            if (endpointSla == TimeSpan.Zero)
                return;

            var timeToSLABreachCalculator = new EstimatedTimeToSLABreachCalculator();


            PerformanceCounter slaBreachCounter;
            try
            {
                slaBreachCounter = new PerformanceCounter(CategoryName, "Time left to SLA breach", Configure.EndpointName, false);

                //access the counter type to force a exception to be thrown if the counter doesn't exists
                var t = slaBreachCounter.CounterType;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("NServiceBus performance counter for Time to SLA breach not set up correctly. Please run the NServiceBus infrastructure installers to rectify this problem.",e);
            }

            timeToSLABreachCalculator.SetCounterAction = d => slaBreachCounter.RawValue = Convert.ToInt32(d);

            timeToSLABreachCalculator.Initialize(endpointSla);

            Configure.Instance.Configurer.RegisterSingleton<EstimatedTimeToSLABreachCalculator>(timeToSLABreachCalculator);
        }

        const string CategoryName = "NServiceBus";
    }
}