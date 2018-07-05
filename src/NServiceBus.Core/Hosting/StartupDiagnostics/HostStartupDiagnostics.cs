namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Features;
    using Logging;
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
                try
                {
                    diagnosticsRootPath = Path.Combine(Host.GetOutputDirectory(), ".diagnostics");
                }
                catch (Exception e)
                {
                    logger.Error("Unable to determine the diagnostics output directory. Check the attached exception for further information, or configure a custom diagnostics directory using 'EndpointConfiguration.SetDiagnosticsPath()'.", e);
                }
            }

            if (!Directory.Exists(diagnosticsRootPath))
            {
                Directory.CreateDirectory(diagnosticsRootPath);
            }

            var endpointName = settings.EndpointName();

            // Once we have the proper hosting model in place we can skip the endpoint name since the host would
            // know how to handle multi hosting but for now we do this so that multi-hosting users will get a file per endpoint
            var startupDiagnosticsFileName = $"{endpointName}-configuration.txt";
            var startupDiagnosticsFilePath = Path.Combine(diagnosticsRootPath, startupDiagnosticsFileName);

            return data => AsyncFile.WriteText(startupDiagnosticsFilePath, data);
        }

        static readonly ILog logger = LogManager.GetLogger<HostStartupDiagnostics>();
    }
}