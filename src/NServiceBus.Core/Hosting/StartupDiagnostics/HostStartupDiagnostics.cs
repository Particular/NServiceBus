namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Features;
    using Logging;
    using Settings;
    using SimpleJson;

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

            context.RegisterStartupTask(new WriteStartupDiagnostics(diagnosticsWriter, settings));
        }

        static HostDiagnosticsWriter GetDiagnosticsWriter(ReadOnlySettings settings)
        {
            if (settings.TryGet<HostDiagnosticsWriter>(out var diagnosticsWriter))
            {
                return diagnosticsWriter;
            }

            if (!settings.TryGet<string>(HostDiagnosticsConfigurationExtensions.DiagnosticsPathKey, out var diagnosticsRootPath))
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

            return new HostDiagnosticsWriter(data => AsyncFile.WriteText(startupDiagnosticsFilePath, data));
        }

        class WriteStartupDiagnostics : FeatureStartupTask
        {
            public WriteStartupDiagnostics(HostDiagnosticsWriter diagnosticsWriter, ReadOnlySettings settings)
            {
                this.diagnosticsWriter = diagnosticsWriter;
                this.settings = settings;
            }

            protected override async Task OnStart(IMessageSession session)
            {
                try
                {
                    var data = SimpleJson.SerializeObject(settings.Get<StartupDiagnosticEntries>().Entries
                        .OrderBy(e=>e.Name)
                        .ToDictionary(e => e.Name, e => e.Data));

                    await diagnosticsWriter.Write(data).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger.Error("Failed to write startup diagnostics", e);
                }
            }

            protected override Task OnStop(IMessageSession session)
            {
                return TaskEx.CompletedTask;
            }

            HostDiagnosticsWriter diagnosticsWriter;
            ReadOnlySettings settings;

            static ILog logger = LogManager.GetLogger<WriteStartupDiagnostics>();
        }
    }
}