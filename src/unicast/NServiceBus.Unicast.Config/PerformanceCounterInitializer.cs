namespace NServiceBus.Unicast.Config
{
    using NServiceBus.Config;
    using Timing;

    /// <summary>
    /// Initializes the peformcecounters if they are enabled
    /// </summary>
    public class PerformanceCounterInitializer:IWantToRunWhenConfigurationIsComplete
    {

        public void Run()
        {
            if (!Configure.Instance.PerformanceCountersEnabled())
                return;

            var counter = new CriticalTimePerformanceCounter();

            counter.Initialize();

            Configure.Instance.Configurer.RegisterSingleton<CriticalTimePerformanceCounter>(counter);
        }



    }
}