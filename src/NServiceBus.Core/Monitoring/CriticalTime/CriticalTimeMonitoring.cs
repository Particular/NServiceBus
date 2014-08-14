namespace NServiceBus.Features
{

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
            SetupCriticalTimePerformanceCounter(context);

            context.Pipeline.Register<CriticalTimeBehavior.Registration>();
        }

        static void SetupCriticalTimePerformanceCounter(FeatureConfigurationContext context)
        {
            var criticalTimeCounter = PerformanceCounterHelper.InstantiateCounter("Critical Time", context.Settings.EndpointName());
            var criticalTimeCalculator = new CriticalTimeCalculator(criticalTimeCounter);
            context.Container.RegisterSingleton(criticalTimeCalculator);
        }

    }
}