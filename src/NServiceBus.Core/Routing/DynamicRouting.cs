namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Routing;
    using NServiceBus.Settings;

    /// <summary>
    /// Provides support for distribution load by routing message dynamically
    /// </summary>
    class DynamicRouting : Feature
    {
        internal DynamicRouting()
        {
            Defaults(s => s.EnableFeatureByDefault(GetSelectedFeatureForRouting(s)));
        }

        static Type GetSelectedFeatureForRouting(SettingsHolder settings)
        {
            var dataBusDefinition = settings.Get<DynamicRoutingDefinition>("SelectedRouting");

            return dataBusDefinition.ProvidedByFeature();
        }

        /// <summary>
        /// Configures the feature
        /// </summary>
        /// <param name="context">The feature context</param>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register<DynamicRoutingBehavior.RoutingDistributorRegistration>();

            context.Container.ConfigureComponent<DynamicRoutingBehavior>(DependencyLifecycle.SingleInstance);
        }
    }
}