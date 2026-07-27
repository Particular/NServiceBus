#nullable enable

namespace NServiceBus;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Logging;

class HostStartupDiagnosticsWriter(Func<string, CancellationToken, Task> diagnosticsWriter, bool isCustomWriter, bool writeDiagnosticsToLog)
{
    public async Task Write(List<StartupDiagnosticEntries.StartupDiagnosticEntry> entries, CancellationToken cancellationToken = default)
    {
        const int LogSafeThreshold = 30000; // Derived from application insights limits

        var resolvedEntries = ResolveEntries(entries);

        if (writeDiagnosticsToLog)
        {
            try
            {
                var data = SerializeToJson(resolvedEntries, forLog: true);
                // Safety net: truncate if still exceeds threshold (e.g., due to other large sections)
                if (data.Length > LogSafeThreshold)
                {
                    Logger.WarnFormat("Startup diagnostics exceeds safe threshold of {0} bytes and will be truncated. Original size: {1} bytes. Consider using CustomDiagnosticsWriter() for full data.", LogSafeThreshold, data.Length);
                    data = string.Concat(data.AsSpan(0, LogSafeThreshold), "... (truncated)");
                }
                Logger.InfoFormat("Startup diagnostics: {0}.", data);
            }
            catch (Exception exception)
            {
                Logger.Error("Failed to serialize startup diagnostics", exception);
                return;
            }
        }

        try
        {
            var data = SerializeToJson(resolvedEntries, forLog: false);
            await diagnosticsWriter(data, cancellationToken).ConfigureAwait(false);
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

    static List<ResolvedEntry> ResolveEntries(List<StartupDiagnosticEntries.StartupDiagnosticEntry> entries)
    {
        var deduplicated = DeduplicateEntries(entries);
        return deduplicated
            .OrderBy(e => e.Name)
            .Select(e =>
            {
                object value;
                if (e.Factory is not null)
                {
                    value = e.Factory();
                }
                else if (e.Data is Func<object> func)
                {
                    value = func();
                }
                else
                {
                    value = e.Data;
                }

                return new ResolvedEntry(e.Name, value, e.JsonTypeInfo);
            })
            .ToList();
    }

    static string SerializeToJson(List<ResolvedEntry> resolvedEntries, bool forLog)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();

        foreach (var entry in resolvedEntries)
        {
            var value = entry.Value;
            var jsonTypeInfo = entry.JsonTypeInfo;

            // Compact AssemblyScanning section only for log output
            if (forLog && jsonTypeInfo == null && value is AssemblyScanningDiagnostics assemblyScanning)
            {
                value = assemblyScanning.CreateCompactedVersion();
            }

            writer.WritePropertyName(entry.Name);

            if (jsonTypeInfo != null)
            {
                // AOT-safe path: use the provided JsonTypeInfo
                JsonSerializer.Serialize(writer, value, jsonTypeInfo);
            }
            else
            {
                // Legacy path: use reflection-based serialization with the custom options
                JsonSerializer.Serialize(writer, value, diagnosticsOptions);
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
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
                    Data = entry.Data,
                    JsonTypeInfo = entry.JsonTypeInfo,
                    Factory = entry.Factory
                };
            }
        }
    }

    readonly struct ResolvedEntry(string name, object value, JsonTypeInfo? jsonTypeInfo)
    {
        public string Name { get; } = name;
        public object Value { get; } = value;
        public JsonTypeInfo? JsonTypeInfo { get; } = jsonTypeInfo;
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
