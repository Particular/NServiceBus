namespace NServiceBus.Features
{
    using System;
    using System.Linq;

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
        /// <see cref="Feature.Setup"/>
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
            context.PipelinesCollection.Register<SLABehavior.Registration>();
        }

        static bool TryGetSLA(FeatureConfigurationContext context, out TimeSpan endpointSla)
        {
            if (context.Settings.TryGet(EndpointSLAKey, out endpointSla))
            {
                return true;
            }

            if (TryGetSlaFromAttribute(context, out endpointSla))
            {
                return true;
            }

            return false;
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
    }
}