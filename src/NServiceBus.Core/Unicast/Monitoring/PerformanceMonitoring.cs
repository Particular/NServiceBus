namespace NServiceBus.Features
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using NServiceBus.Unicast.Monitoring;

    /// <summary>
    /// Used to configure PerformanceMonitoring.
    /// </summary>
    public class PerformanceMonitoring : Feature
    {

        internal PerformanceMonitoring()
        {
        }

        /// <summary>
        /// <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            SetupCriticalTimePerformanceCounter(context);
            SetupSLABreachCounter(context);
            context.Container.ConfigureComponent<ProcessingStatistics>(DependencyLifecycle.InstancePerUnitOfWork);
        }

        static void SetupCriticalTimePerformanceCounter(FeatureConfigurationContext context)
        {
            var criticalTimeCounter = InstantiateCounter(context, "Critical Time");
            var criticalTimeCalculator = new CriticalTimeCalculator(criticalTimeCounter);
            context.Container.RegisterSingleton(criticalTimeCalculator);
        }

        static void SetupSLABreachCounter(FeatureConfigurationContext context)
        {
            TimeSpan endpointSla;
            if (!TryGetSla(context, out endpointSla))
            {
                return;
            }

            var slaBreachCounter = InstantiateCounter(context, "SLA violation countdown");
            var timeToSLABreachCalculator = new EstimatedTimeToSLABreachCalculator(endpointSla, slaBreachCounter);
            context.Container.RegisterSingleton(timeToSLABreachCalculator);
        }

        static bool TryGetSla(FeatureConfigurationContext context, out TimeSpan endpointSla)
        {
            if (context.Settings.TryGetEndpointSLA(out endpointSla))
            {
                return true;
            }
            return TryGetSlaFromAttribute(context, out endpointSla);
        }

        static bool TryGetSlaFromAttribute(FeatureConfigurationContext config, out TimeSpan sla)
        {
            var configType = config.Settings
                .GetAvailableTypes()
                .SingleOrDefault(t => typeof(IConfigureThisEndpoint).IsAssignableFrom(t) && !t.IsInterface);

            if (configType == null)
            {
                sla = TimeSpan.Zero;
                return false;
            }

            var endpointSLAAttribute = (EndpointSLAAttribute)configType.GetCustomAttributes(typeof(EndpointSLAAttribute), false)
                .FirstOrDefault();
            if (endpointSLAAttribute == null)
            {
                sla = TimeSpan.Zero;
                return false;
            }

            sla = endpointSLAAttribute.SLA;
            return true;
        }

        static PerformanceCounter InstantiateCounter(FeatureConfigurationContext context, string counterName)
        {
            try
            {
                var counter = new PerformanceCounter(CategoryName, counterName, context.Settings.EndpointName(), false);
                //access the counter type to force a exception to be thrown if the counter doesn't exists
                // ReSharper disable once UnusedVariable
                var t = counter.CounterType;
                return counter;
            }
            catch (Exception e)
            {
                var message = string.Format("NServiceBus performance counter for {0} is not set up correctly. Please run Install-NServiceBusPerformanceCounters cmdlet to rectify this problem.", counterName);
                throw new InvalidOperationException(message, e);
            }
        }

        const string CategoryName = "NServiceBus";
    }
}