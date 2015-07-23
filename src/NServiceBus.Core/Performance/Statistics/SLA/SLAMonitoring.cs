namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Performance.Counters;

    /// <summary>
    /// Used to configure SLAMonitoring.
    /// </summary>
    public class SLAMonitoring : Feature
    {
        internal const string EndpointSLAKey = "EndpointSLA";

        internal SLAMonitoring()
        {
        }

        /// <summary>
        /// <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                return;
            }

            TimeSpan endpointSla;
            if (!TryGetSLA(context, out endpointSla))
            {
                return;
            }

            var slaBreachCounter = PerformanceCounterHelper.InstantiatePerformanceCounter("SLA violation countdown", context.Settings.EndpointName());
            var timeToSLABreachCalculator = new EstimatedTimeToSLABreachCalculator(endpointSla, slaBreachCounter);
            context.Container.RegisterSingleton(timeToSLABreachCalculator);
            context.Pipeline.Register<SLABehavior.Registration>();
        }

        static bool TryGetSLA(FeatureConfigurationContext context, out TimeSpan endpointSla)
        {
            if (context.Settings.TryGet(EndpointSLAKey, out endpointSla))
            {
                return true;
            }

            return false;
        }

    }
}