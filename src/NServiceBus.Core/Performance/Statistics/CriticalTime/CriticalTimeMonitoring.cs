namespace NServiceBus.Features
{
    using System.Collections.Generic;
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
        /// <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            var criticalTimeCounter = PerformanceCounterHelper.InstantiatePerformanceCounter("Critical Time", context.Settings.EndpointName().ToString());
            var criticalTimeCalculator = new CriticalTimeCalculator(criticalTimeCounter);
            context.Container.RegisterSingleton(criticalTimeCalculator);
            context.Pipeline.Register<CriticalTimeBehavior.Registration>();
            return FeatureStartupTask.None;
        }
    }
}