namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using SimpleJson;

    class HostStartupDiagnosticsWriter
    {
        public HostStartupDiagnosticsWriter(Func<string, CancellationToken, Task> diagnosticsWriter, bool isCustomWriter)
        {
            this.diagnosticsWriter = diagnosticsWriter;
            this.isCustomWriter = isCustomWriter;
        }

        public async Task Write(List<StartupDiagnosticEntries.StartupDiagnosticEntry> entries, CancellationToken cancellationToken = default)
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
                await diagnosticsWriter(data, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
            {
                if (isCustomWriter)
                {
                    logger.Error($"Failed to write startup diagnostics using the custom delegate defined by {nameof(DiagnosticSettingsExtensions.CustomDiagnosticsWriter)}", ex);
                    return;
                }
                logger.Error("Failed to write startup diagnostics", ex);
            }
        }

        IEnumerable<StartupDiagnosticEntries.StartupDiagnosticEntry> DeduplicateEntries(List<StartupDiagnosticEntries.StartupDiagnosticEntry> entries)
        {
            var countMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in entries)
            {
                if (!countMap.ContainsKey(entry.Name))
                {
                    countMap.Add(entry.Name, 1);
                    yield return entry;
                }
                else
                {
                    countMap[entry.Name] += 1;
                    var entryNewName = $"{entry.Name}-{countMap[entry.Name]}";

                    logger.Warn($"A duplicate diagnostic entry was renamed from {entry.Name} to {entryNewName}.");

                    yield return new StartupDiagnosticEntries.StartupDiagnosticEntry
                    {
                        Name = entryNewName,
                        Data = entry.Data
                    };
                }
            }
        }

        Func<string, CancellationToken, Task> diagnosticsWriter;
        bool isCustomWriter;

        static ILog logger = LogManager.GetLogger<HostStartupDiagnosticsWriter>();
    }
}