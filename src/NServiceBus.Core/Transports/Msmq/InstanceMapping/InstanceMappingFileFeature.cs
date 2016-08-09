namespace NServiceBus.Features
{
    using System;
    using System.IO;
    using Routing;

    class InstanceMappingFileFeature : Feature
    {
        public InstanceMappingFileFeature()
        {
            Defaults(s =>
            {
                s.SetDefault(CheckIntervalSettingsKey, TimeSpan.FromSeconds(30));
                s.SetDefault(FilePathSettingsKey, DefaultInstanceMappingFileName);
            });
            Prerequisite(c => c.Settings.HasExplicitValue(FilePathSettingsKey) || File.Exists(GetRootedPath(DefaultInstanceMappingFileName)), "No explicit instance mapping file configuration and default file does not exist.");
            DependsOn<RoutingFeature>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var filePath = GetRootedPath(context.Settings.Get<string>(FilePathSettingsKey));

            if (!File.Exists(filePath))
            {
                throw new Exception($"The specified instance mapping file '{filePath}' does not exist.");
            }

            var checkInterval = context.Settings.Get<TimeSpan>(CheckIntervalSettingsKey);
            var endpointInstances = context.Settings.Get<EndpointInstances>();

            var instanceMappingTable = new InstanceMappingTable(filePath, checkInterval, new AsyncTimer(), new InstanceMappingFileAccess());
            instanceMappingTable.ReloadData();
            endpointInstances.AddDynamic(instanceMappingTable.FindInstances);
            context.RegisterStartupTask(instanceMappingTable);
        }

        static string GetRootedPath(string filePath)
        {
            return Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
        }

        public const string CheckIntervalSettingsKey = "FileBasedRouting.CheckInterval";
        public const string FilePathSettingsKey = "FileBasedRouting.FilePath";
        const string DefaultInstanceMappingFileName = "instance-mapping.xml";
    }
}