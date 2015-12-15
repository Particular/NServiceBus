namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using NServiceBus.Settings;

    class FileRoutingTableFeature : Feature
    {
        public FileRoutingTableFeature()
        {
            DependsOn<RoutingFeature>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var fileRoutingTable = new FileRoutingTable(context.Settings.Get<string>("FileBasedRouting.BasePath"), TimeSpan.FromSeconds(5));
            context.Container.RegisterSingleton(fileRoutingTable);

            context.RegisterStartupTask(new StartupTask(context.Settings, fileRoutingTable));
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

            protected override Task OnStart(IBusSession session)
            {
                settings.Get<EndpointInstances>().AddDynamic(e => routingTable.GetInstances(e));
                return TaskEx.Completed;
            }

            protected override Task OnStop(IBusSession session)
            {
                return TaskEx.Completed;
            }
        }
    }
}