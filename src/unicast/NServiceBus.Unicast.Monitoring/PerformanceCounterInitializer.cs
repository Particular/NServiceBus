namespace NServiceBus.Unicast.Monitoring
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Initializes the peformcecounters if they are enabled
    /// </summary>
    public class PerformanceCounterInitializer : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            if (!Configure.Instance.PerformanceCountersEnabled())
                return;

            if (!PerformanceCounterCategory.Exists(CategoryName))
            {
                return;
            }

            SetupCriticalTimePerformanceCounter();

            SetupSLABreachCounter();
        }

        static void SetupCriticalTimePerformanceCounter()
        {
            var criticalTimeCalculator = new CriticalTimeCalculator();

            var criticalTimeCounter = InstantiateCounter("Critical Time");

            criticalTimeCalculator.Initialize(criticalTimeCounter);

            Configure.Instance.Configurer.RegisterSingleton<CriticalTimeCalculator>(criticalTimeCalculator);
        }

        static void SetupSLABreachCounter()
        {
            var endpointSla = Configure.Instance.EndpointSLA();

            if (endpointSla == TimeSpan.Zero)
                return;

            var timeToSLABreachCalculator = new EstimatedTimeToSLABreachCalculator();


            var slaBreachCounter = InstantiateCounter("SLA violation countdown");

            timeToSLABreachCalculator.SetCounterAction = d =>
                                                             {
                                                                 slaBreachCounter.RawValue = Convert.ToInt32(Math.Min(d, Int32.MaxValue));
                                                             };

            timeToSLABreachCalculator.Initialize(endpointSla);

            Configure.Instance.Configurer.RegisterSingleton<EstimatedTimeToSLABreachCalculator>(timeToSLABreachCalculator);
        }

        static PerformanceCounter InstantiateCounter(string counterName)
        {
            PerformanceCounter counter;
            
            try
            {
                counter = new PerformanceCounter(CategoryName, counterName, Configure.EndpointName, false);

                //access the counter type to force a exception to be thrown if the counter doesn't exists
                var t = counter.CounterType;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    string.Format("NServiceBus performance counter for {0} not set up correctly. Please run the NServiceBus infrastructure installers to rectify this problem.",counterName),
                    e);
            }
            return counter;
        }

        const string CategoryName = "NServiceBus";
    }
}