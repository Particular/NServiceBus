namespace NServiceBus.Unicast.Monitoring
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Initializes the performance counters if they are enabled
    /// </summary>
    class PerformanceCounterInitializer : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run(Configure config)
        {
            if (!config.PerformanceCountersEnabled())
                return;

            if (!PerformanceCounterCategory.Exists(CategoryName))
            {
                return;
            }

            SetupCriticalTimePerformanceCounter(config);

            SetupSLABreachCounter(config);
        }

        static void SetupCriticalTimePerformanceCounter(Configure config)
        {
            var criticalTimeCalculator = new CriticalTimeCalculator();
            var criticalTimeCounter = InstantiateCounter("Critical Time");

            criticalTimeCalculator.Initialize(criticalTimeCounter);

            config.Configurer.RegisterSingleton<CriticalTimeCalculator>(criticalTimeCalculator);
        }

        static void SetupSLABreachCounter(Configure config)
        {
            var endpointSla = config.EndpointSLA();

            if (endpointSla == TimeSpan.Zero)
                return;

            var timeToSLABreachCalculator = new EstimatedTimeToSLABreachCalculator();
            var slaBreachCounter = InstantiateCounter("SLA violation countdown");

            timeToSLABreachCalculator.Initialize(endpointSla, slaBreachCounter);

            config.Configurer.RegisterSingleton<EstimatedTimeToSLABreachCalculator>(timeToSLABreachCalculator);
        }

        static PerformanceCounter InstantiateCounter(string counterName)
        {
            PerformanceCounter counter;
            
            try
            {
                counter = new PerformanceCounter(CategoryName, counterName, Configure.EndpointName, false);
                //access the counter type to force a exception to be thrown if the counter doesn't exists
                // ReSharper disable once UnusedVariable
                var t = counter.CounterType; 
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    string.Format("NServiceBus performance counter for {0} is not set up correctly. Please run Install-NServiceBusPerformanceCounters cmdlet to rectify this problem.", counterName),
                    e);
            }
            return counter;
        }

        const string CategoryName = "NServiceBus";
    }
}