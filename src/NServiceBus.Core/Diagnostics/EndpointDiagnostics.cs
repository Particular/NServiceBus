namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Features;
    using Logging;

    class EndpointDiagnostics : Feature
    {
        public EndpointDiagnostics()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var diagnosticsWriter = GetDiagnosticsWriter(context);

            context.RegisterStartupTask(new WriteStartupDiagnostics(diagnosticsWriter));
        }


        static DiagnosticsWriter GetDiagnosticsWriter(FeatureConfigurationContext context)
        {
            var settings = context.Settings;

            if (settings.TryGet<DiagnosticsWriter>(out var diagnosticsWriter))
            {
                return diagnosticsWriter;
            }

            if (!settings.TryGet<string>(DiagnosticsConfigurationExtensions.DiagnosticsRootPathKey, out var diagnosticsRootPath))
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

            return new DiagnosticsWriter(data => AsyncFile.WriteText(startupDiagnoticsFilePath, data));
        }

        class WriteStartupDiagnostics : FeatureStartupTask
        {
            public WriteStartupDiagnostics(DiagnosticsWriter diagnosticsWriter)
            {
                this.diagnosticsWriter = diagnosticsWriter;
            }

            protected override async Task OnStart(IMessageSession session)
            {
                var data = "tbd";

                try
                {
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

            DiagnosticsWriter diagnosticsWriter;

            static ILog logger = LogManager.GetLogger<WriteStartupDiagnostics>();
        }
    }
}