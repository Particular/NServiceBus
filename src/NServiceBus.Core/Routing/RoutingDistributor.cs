namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Routing;
    using NServiceBus.Settings;

    /// <summary>
    /// Provides support for distribution load by routing message dynamically
    /// </summary>
    public class RoutingDistributor : Feature
    {
        internal RoutingDistributor()
        {
            Func<Address, string> translator = a => a.Queue;
            
            Defaults(s => s.SetDefault("Routing.Translator", translator));

            Defaults(s => s.EnableFeatureByDefault(GetSelectedFeatureForRouting(s)));
        }

        static Type GetSelectedFeatureForRouting(SettingsHolder settings)
        {
            var dataBusDefinition = settings.Get<RoutingDistributorDefinition>("SelectedRouting");

            return dataBusDefinition.ProvidedByFeature();
        }

        /// <summary>
        /// Configures the feature
        /// </summary>
        /// <param name="context">The feature context</param>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register<RoutingDistributorBehavior.RoutingDistributorRegistration>();
            
            context.Container.ConfigureComponent<RoutingDistributorBehavior>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(b => b.Translator, context.Settings.Get("Routing.Translator"));
        }
    }
}