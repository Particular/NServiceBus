namespace NServiceBus.Features
{
    using System;
    using System.IO;
    using Routing;

    class FileRoutingTableFeature : Feature
    {
        public FileRoutingTableFeature()
        {
            Defaults(s =>
            {
                s.SetDefault(CheckIntervalSettingsKey, TimeSpan.FromSeconds(30));
                s.SetDefault(MaxLoadAttemptsSettingsKey, 10);
                s.SetDefault(FilePathSettingsKey, "instance-mapping.xml");
            });
            DependsOn<RoutingFeature>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var filePath = context.Settings.Get<string>(FilePathSettingsKey);
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
            }

            if (!File.Exists(filePath))
            {
                return;
            }

            var checkInterval = context.Settings.Get<TimeSpan>(CheckIntervalSettingsKey);
            var maxLoadAttempts = context.Settings.Get<int>(MaxLoadAttemptsSettingsKey);

            var endpointInstances = context.Settings.Get<EndpointInstances>();

            var fileRoutingTable = new FileRoutingTable(filePath, checkInterval, new AsyncTimer(), new RoutingFileAccess(), maxLoadAttempts);
            //TODO fix this:
            fileRoutingTable.ReloadData().GetAwaiter().GetResult();
            endpointInstances.AddDynamic(fileRoutingTable.FindInstances);
            context.RegisterStartupTask(fileRoutingTable);
        }

        public const string CheckIntervalSettingsKey = "FileBasedRouting.CheckInterval";
        public const string MaxLoadAttemptsSettingsKey = "FileBasedRouting.MaxLoadAttempts";
        public const string FilePathSettingsKey = "FileBasedRouting.FilePath";
    }
}