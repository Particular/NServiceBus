namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Routing;
    using NServiceBus.Settings;

    class FileRoutingTableFeature : Feature
    {
        public FileRoutingTableFeature()
        {
            DependsOn<RoutingFeature>();
            RegisterStartupTask<StartupTask>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.RegisterSingleton(new FileRoutingTable(context.Settings.Get<string>("FileBasedRouting.BasePath"), TimeSpan.FromSeconds(5)));
        }

        class StartupTask : FeatureStartupTask
        {
            ReadOnlySettings settings;
            FileRoutingTable routingTable;

            public StartupTask(ReadOnlySettings settings, FileRoutingTable routingTable)
            {
                this.settings = settings;
                this.routingTable = routingTable;
            }

            protected override void OnStart()
            {
                settings.Get<EndpointInstances>().AddDynamic(e => routingTable.GetInstances(e));
            }
        }
    }
}