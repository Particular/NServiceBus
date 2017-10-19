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
        public WriteStartupDiagnostics(Func<string, Task> diagnosticsWriter, ReadOnlySettings settings, bool isCustomWriter)
        {
            this.diagnosticsWriter = diagnosticsWriter;
            this.settings = settings;
            this.isCustomWriter = isCustomWriter;
        }

        protected override async Task OnStart(IMessageSession session)
        {
            var entries = settings.Get<StartupDiagnosticEntries>().Entries;

            var duplicateNames = entries.GroupBy(item => item.Name)
                .Where(group => group.Count() > 1)
                .ToList();
            if (duplicateNames.Any())
            {
                logger.Error("Diagnostics entries contains duplicates. Duplicates: " + string.Join(", ", duplicateNames));
                return;
            }

            var dictionary = entries
                .OrderBy(e => e.Name)
                .ToDictionary(e => e.Name, e => e.Data);
            string data;

            try
            {
                data = SimpleJson.SerializeObject(dictionary);
            }
            catch (Exception exception)
            {
                logger.Error("Failed to serialize startup diagnostics", exception);
                return;
            }
            try
            {
                await diagnosticsWriter(data)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (isCustomWriter)
                {
                    logger.Error($"Failed to write startup diagnostics using the custom delegate defined by {nameof(DiagnosticSettingsExtensions.CustomDiagnosticsWriter)}", exception);
                    return;
                }
                logger.Error("Failed to write startup diagnostics", exception);
            }
        }

        protected override Task OnStop(IMessageSession session)
        {
            return TaskEx.CompletedTask;
        }

        Func<string, Task> diagnosticsWriter;
        ReadOnlySettings settings;
        bool isCustomWriter;

        static ILog logger = LogManager.GetLogger<WriteStartupDiagnostics>();
    }
}