namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Unicast.Routing;

    class RoutingDistributor : Feature
    {
        public RoutingDistributor()
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

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register<RoutingDistributorRegistration>();
            
            context.Container.ConfigureComponent<RoutingDistributorBehavior>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(b => b.Translator, context.Settings.Get("Routing.Translator"));
        }

        public class RoutingDistributorRegistration : RegisterStep
        {
            public RoutingDistributorRegistration()
                : base("RoutingDistributor", typeof(RoutingDistributorBehavior), "Changes destinations address to round robin on workers")
            {
                InsertAfter(WellKnownStep.MutateOutgoingTransportMessage);
                InsertAfterIfExists("LogOutgoingMessage");
                InsertBefore(WellKnownStep.DispatchMessageToTransport);
            }
        }
    }
}