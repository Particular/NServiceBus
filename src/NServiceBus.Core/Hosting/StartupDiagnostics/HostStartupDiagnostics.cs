namespace NServiceBus
{
    using System;
    using System.IO;
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

            if (!settings.TryGet<string>(HostDiagnosticsConfigurationExtensions.DiagnosticsRootPathKey, out var diagnosticsRootPath))
            {
                diagnosticsRootPath = Path.Combine(DefaultFactory.FindDefaultLoggingDirectory(), ".diagnostics");
            }

            if (Directory.Exists(diagnosticsRootPath))
            {
                Directory.CreateDirectory(diagnosticsRootPath);
            }

            if (settings.TryGet<string>("EndpointInstanceDiscriminator", out var discriminator))
            {
                discriminator = "-" + discriminator;
            }

            var startupDiagnoticsFileName = $"{settings.EndpointName()}{discriminator ?? ""}-config.txt";
            var startupDiagnoticsFilePath = Path.Combine(diagnosticsRootPath, startupDiagnoticsFileName);

            if (File.Exists(startupDiagnoticsFilePath))
            {
                File.Delete(startupDiagnoticsFilePath);
            }

            return new HostDiagnosticsWriter(data => AsyncFile.WriteText(startupDiagnoticsFilePath, data));
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
                    var data = "";

                    foreach (var section in settings.Get<StartupDiagnosticEntries>().Entries)
                    {
                        var sectionData = SimpleJson.SerializeObject(section.Data);

                        if (!string.IsNullOrEmpty(data))
                        {
                            data += "," + Environment.NewLine;
                        }
                        data += $"{section.Name}: {sectionData}";
                    }

                    await diagnosticsWriter.Write("{" + Environment.NewLine + data + "}").ConfigureAwait(false);
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