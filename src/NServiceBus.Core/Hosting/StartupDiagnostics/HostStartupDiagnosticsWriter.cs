namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Logging;
    using SimpleJson;

    class HostStartupDiagnosticsWriter
    {
        public HostStartupDiagnosticsWriter(Func<string, Task> diagnosticsWriter, bool isCustomWriter)
        {
            this.diagnosticsWriter = diagnosticsWriter;
            this.isCustomWriter = isCustomWriter;
        }

        public async Task Write(List<StartupDiagnosticEntries.StartupDiagnosticEntry> entries)
        {
            var duplicateNames = entries.GroupBy(item => item.Name)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
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

        Func<string, Task> diagnosticsWriter;
        bool isCustomWriter;

        static ILog logger = LogManager.GetLogger<HostStartupDiagnosticsWriter>();
    }
}