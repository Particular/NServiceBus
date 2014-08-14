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

            SetupSLABreachCounter(context);

            context.Pipeline.Register<SLABehavior.Registration>();
        }

        static void SetupSLABreachCounter(FeatureConfigurationContext context)
        {
            var endpointSla = GetSla(context);
            var slaBreachCounter = PerformanceCounterHelper.InstantiateCounter("SLA violation countdown", context.Settings.EndpointName());
            var timeToSLABreachCalculator = new EstimatedTimeToSLABreachCalculator(endpointSla, slaBreachCounter);
            context.Container.RegisterSingleton(timeToSLABreachCalculator);
        }

        static TimeSpan GetSla(FeatureConfigurationContext context)
        {
            TimeSpan endpointSla;
            if (context.Settings.TryGet(EndpointSLAKey, out endpointSla))
            {
                return endpointSla;
            }
            if (TryGetSlaFromAttribute(context, out endpointSla))
            {
                return endpointSla;
            }
            throw new Exception("Could not extract SLA from settings or attribute. Please either call ConfigurationBuilder.EnableSla or add a EndpointSLAAttribute to your IConfigureThisEndpoint.");
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