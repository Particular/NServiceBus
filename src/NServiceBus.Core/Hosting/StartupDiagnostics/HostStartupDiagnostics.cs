namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Features;
    using Settings;

    class HostStartupDiagnostics : Feature
    {
        public HostStartupDiagnostics()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var settings = context.Settings;

            var diagnosticsWriter = GetDiagnosticsWriter(settings);

            context.RegisterStartupTask(diagnosticsWriter);
        }

        static WriteStartupDiagnostics GetDiagnosticsWriter(ReadOnlySettings settings)
        {
            if (settings.TryGetCustomDiagnosticsWriter(out var diagnosticsWriter))
            {
                return new WriteStartupDiagnostics(diagnosticsWriter, settings, true);
            }

            var defaultDiagnosticsWriter = BuildDefaultDiagnosticsWriter(settings);
            return new WriteStartupDiagnostics(defaultDiagnosticsWriter, settings, false);
        }

        static Func<string, Task> BuildDefaultDiagnosticsWriter(ReadOnlySettings settings)
        {
            if (!settings.TryGet<string>(DiagnosticSettingsExtensions.DiagnosticsPathKey, out var diagnosticsRootPath))
            {
                diagnosticsRootPath = Path.Combine(Host.GetOutputDirectory(), ".diagnostics");
            }

            if (!Directory.Exists(diagnosticsRootPath))
            {
                Directory.CreateDirectory(diagnosticsRootPath);
            }

            var endpointName = settings.EndpointName();

            // Once we have the proper hosting model in place we can skip the endpoint name since the host would
            // know how to handle multi hosting but for now we do this so that multi-hosting users will get a file per endpoint
            var startupDiagnosticsFileName = $"{endpointName}-configuration.json";
            var startupDiagnosticsFilePath = Path.Combine(diagnosticsRootPath, startupDiagnosticsFileName);

            return data => AsyncFile.WriteText(startupDiagnosticsFilePath, data);
        }
    }
}