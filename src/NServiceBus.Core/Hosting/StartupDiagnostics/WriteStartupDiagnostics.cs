namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Features;
    using Logging;
    using Settings;
    using SimpleJson;

    class WriteStartupDiagnostics : FeatureStartupTask
    {
        public WriteStartupDiagnostics(Func<string, Task> diagnosticsWriter, ReadOnlySettings settings)
        {
            this.diagnosticsWriter = diagnosticsWriter;
            this.settings = settings;
        }

        protected override async Task OnStart(IMessageSession session)
        {
            try
            {
                var data = SimpleJson.SerializeObject(settings.Get<StartupDiagnosticEntries>().Entries
                    .OrderBy(e => e.Name)
                    .ToDictionary(e => e.Name, e => e.Data));

                await diagnosticsWriter(data).ConfigureAwait(false);
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

        Func<string, Task> diagnosticsWriter;
        ReadOnlySettings settings;

        static ILog logger = LogManager.GetLogger<WriteStartupDiagnostics>();
    }
}