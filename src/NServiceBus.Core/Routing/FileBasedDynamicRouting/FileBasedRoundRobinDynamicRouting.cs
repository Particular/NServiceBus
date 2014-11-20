namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Routing;

    class FileBasedRoundRobinDynamicRouting : Feature
    {
        public FileBasedRoundRobinDynamicRouting()
        {
            DependsOn<DynamicRouting>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.RegisterSingleton<IProvideDynamicRouting>(new FileBasedRoundRobinRoutingProvider(context.Settings.Get<string>("FileBasedRouting.BasePath"), TimeSpan.FromSeconds(5)));
        }
    }
}