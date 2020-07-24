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
            var deduplicatedEntries = DeduplicateEntries(entries);
            var dictionary = deduplicatedEntries
                .OrderBy(e => e.Name)
                .ToDictionary(e => e.Name, e => e.Data, StringComparer.OrdinalIgnoreCase);

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

        List<StartupDiagnosticEntries.StartupDiagnosticEntry> DeduplicateEntries(List<StartupDiagnosticEntries.StartupDiagnosticEntry> entries)
        {
            var countMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var newList = new List<StartupDiagnosticEntries.StartupDiagnosticEntry>();
            
            foreach (var entry in entries)
            {
                if (!countMap.ContainsKey(entry.Name))
                {
                    countMap.Add(entry.Name, 1);
                    newList.Add(entry);
                }
                else
                {
                    countMap[entry.Name] += 1;
                    newList.Add(new StartupDiagnosticEntries.StartupDiagnosticEntry
                    {
                        Name = $"{entry.Name}-{countMap[entry.Name]}",
                        Data = entry.Data
                    });
                }
            }

            return newList;
        }

        Func<string, Task> diagnosticsWriter;
        bool isCustomWriter;

        static ILog logger = LogManager.GetLogger<HostStartupDiagnosticsWriter>();
    }
}