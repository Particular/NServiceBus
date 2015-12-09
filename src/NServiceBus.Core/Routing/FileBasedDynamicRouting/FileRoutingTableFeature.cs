namespace NServiceBus.Features
{
    using System;

    class FileRoutingTableFeature : Feature
    {
        public const string CheckIntervalSettingsKey = "FileBasedRouting.CheckInterval";
        public const string MaxLoadAttemptsSettingsKey = "FileBasedRouting.MaxLoadAttempts";
        public const string FilePathSettingsKey = "FileBasedRouting.FilePath";

        public FileRoutingTableFeature()
        {
            Defaults(s =>
            {
                s.SetDefault(CheckIntervalSettingsKey, TimeSpan.FromSeconds(30));
                s.SetDefault(MaxLoadAttemptsSettingsKey, 10);
            });
            DependsOn<RoutingFeature>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var filePath = context.Settings.Get<string>(FilePathSettingsKey);
            var checkInterval = context.Settings.Get<TimeSpan>(CheckIntervalSettingsKey);
            var maxLoadAttempts = context.Settings.Get<int>(MaxLoadAttemptsSettingsKey);

            var fileRoutingTable = new FileRoutingTable(filePath, checkInterval, new AsyncTimer(), new RoutingFileAccess(), maxLoadAttempts, context.Settings);
            context.RegisterStartupTask(fileRoutingTable);
        }
    }
}