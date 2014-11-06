namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Unicast.Routing;

    class FileBasedRouterDistribution : Feature
    {
        public FileBasedRouterDistribution()
        {
            DependsOn<RoutingDistributor>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.RegisterSingleton(new FileBasedRoundRobinRoutingDistributor(context.Settings.Get<string>("FileBasedRouting.BasePath"), TimeSpan.FromSeconds(5)));
        }
    }
}