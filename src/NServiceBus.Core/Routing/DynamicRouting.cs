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
            Func<Address, string> translator = a => a.ToString();
            
            Defaults(s => s.SetDefault("Routing.Translator", translator));

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
            context.Pipeline.Register<DisconnectMessageBehavior.DisconnectMessageRegistration>();

            context.Container.ConfigureComponent<NoMessageBacklogNotifier>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<DynamicRoutingBehavior>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(b => b.Translator, context.Settings.Get("Routing.Translator"));
        }
    }
}