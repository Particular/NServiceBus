namespace NServiceBus.Features
{
    using NServiceBus.Performance.Counters;

    /// <summary>
    /// Used to configure CriticalTimeMonitoring.
    /// </summary>
    public class CriticalTimeMonitoring : Feature
    {
        internal CriticalTimeMonitoring()
        {
        }

        /// <summary>
        /// <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var criticalTimeCounter = PerformanceCounterHelper.InstantiatePerformanceCounter("Critical Time", context.Settings.EndpointName());
            var criticalTimeCalculator = new CriticalTimeCalculator(criticalTimeCounter);
            context.Container.RegisterSingleton(criticalTimeCalculator);
            context.Pipeline.Register<CriticalTimeBehavior.Registration>();
        }
    }
}