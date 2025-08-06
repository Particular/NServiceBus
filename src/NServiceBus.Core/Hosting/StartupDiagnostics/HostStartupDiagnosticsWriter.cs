#nullable enable
namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Logging;

class HostStartupDiagnosticsWriter(Func<string, CancellationToken, Task> diagnosticsWriter, bool isCustomWriter)
{
    public async Task Write(List<StartupDiagnosticEntries.StartupDiagnosticEntry> entries, CancellationToken cancellationToken = default)
    {
        var deduplicatedEntries = DeduplicateEntries(entries);
        var dictionary = deduplicatedEntries
            .OrderBy(e => e.Name)
            .ToDictionary(e => e.Name, e => e.Data, StringComparer.OrdinalIgnoreCase);

        string data;

        try
        {
            data = JsonSerializer.Serialize(dictionary, diagnosticsOptions);
        }
        catch (Exception exception)
        {
            Logger.Error("Failed to serialize startup diagnostics", exception);
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
                Logger.Error($"Failed to write startup diagnostics using the custom delegate defined by {nameof(DiagnosticSettingsExtensions.CustomDiagnosticsWriter)}", ex);
                return;
            }
            Logger.Error("Failed to write startup diagnostics", ex);
        }
    }

    static IEnumerable<StartupDiagnosticEntries.StartupDiagnosticEntry> DeduplicateEntries(List<StartupDiagnosticEntries.StartupDiagnosticEntry> entries)
    {
        var countMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (countMap.TryAdd(entry.Name, 1))
            {
                yield return entry;
            }
            else
            {
                countMap[entry.Name] += 1;
                var entryNewName = $"{entry.Name}-{countMap[entry.Name]}";

                Logger.Warn($"A duplicate diagnostic entry was renamed from {entry.Name} to {entryNewName}.");

                yield return new StartupDiagnosticEntries.StartupDiagnosticEntry
                {
                    Name = entryNewName,
                    Data = entry.Data
                };
            }
        }
    }

    static readonly JsonSerializerOptions diagnosticsOptions = new()
    {
        Converters = { new TypeConverter() }
    };

    /// <summary>
    /// By default System.Text.Json would throw with "Serialization and deserialization of 'System.Type' instances are not supported" which normally
    /// would make sense because it can be considered unsafe to serialize and deserialize types. We add a custom converter here to make
    /// sure when diagnostics entries accidentally use types it will just print the full name as a string. We never intent to read these things
    /// back so this is a safe approach.
    /// </summary>
    sealed class TypeConverter : JsonConverter<Type>
    {
        // we never need to deserialize
        public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options) => writer.WriteStringValue(value.FullName);
    }


    static readonly ILog Logger = LogManager.GetLogger<HostStartupDiagnosticsWriter>();
}