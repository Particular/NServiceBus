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
            if (context.Settings.TryGet<DiagnosticsWriter>(out var diagnosticsWriter))
            {
                return diagnosticsWriter;
            }

            if (!context.Settings.TryGet<string>(DiagnosticsConfigurationExtensions.DiagnosticsRootPathKey, out var diagnosticsRootPath))
            {
                diagnosticsRootPath = Path.Combine(DefaultFactory.FindDefaultLoggingDirectory(), ".diagnostics"); //for now
            }

            if (!Directory.Exists(diagnosticsRootPath))
            {
                Directory.CreateDirectory(diagnosticsRootPath);
            }

            var startupDiagnoticsFileName = $"{context.Settings.EndpointName()}-settings-{DateTime.UtcNow.Ticks}.txt"; //todo: figure out better name
            var startupDiagnoticsFilePath = Path.Combine(diagnosticsRootPath, startupDiagnoticsFileName);


            return new DiagnosticsWriter(data => AsyncFile.WriteText(startupDiagnoticsFilePath, data));
        }

        class WriteStartupDiagnostics : FeatureStartupTask
        {
            public WriteStartupDiagnostics(DiagnosticsWriter diagnosticsWriter)
            {
                this.diagnosticsWriter = diagnosticsWriter;
            }

            protected override Task OnStart(IMessageSession session)
            {
                var data = "tbd";

                return diagnosticsWriter.Write(data);
            }

            protected override Task OnStop(IMessageSession session)
            {
                return TaskEx.CompletedTask;
            }

            DiagnosticsWriter diagnosticsWriter;
        }
    }
}