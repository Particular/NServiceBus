#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sagas;

class SagaStorageFile : IDisposable, IAsyncDisposable
{
    SagaStorageFile(FileStream fileStream, JsonSerializerOptions options)
    {
        this.fileStream = fileStream;
        this.options = options;
    }

    public void Dispose()
    {
        if (fileStream is null)
        {
            return;
        }

        fileStream.Close();

        if (isCompleted)
        {
            File.Delete(fileStream.Name);
        }

        fileStream.Dispose(); // Already closed, but for completeness
        fileStream = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (fileStream is null)
        {
            return;
        }

        fileStream.Close();

        if (isCompleted)
        {
            File.Delete(fileStream.Name);
        }

        await fileStream.DisposeAsync().ConfigureAwait(false); // Already closed, but for completeness
        fileStream = null;
    }

    public static Task<SagaStorageFile?> Open(Guid sagaId, SagaManifest manifest, CancellationToken cancellationToken = default)
    {
        var filePath = manifest.GetFilePath(sagaId);

        if (!File.Exists(filePath))
        {
            return noSagaFoundResult;
        }

        return OpenWithRetryOnConcurrency(filePath, FileMode.Open, manifest.SerializerOptions, cancellationToken)!;
    }

    public static Task<SagaStorageFile> Create(Guid sagaId, SagaManifest manifest, CancellationToken cancellationToken = default)
    {
        var filePath = manifest.GetFilePath(sagaId);

        return OpenWithRetryOnConcurrency(filePath, FileMode.CreateNew, manifest.SerializerOptions, cancellationToken);
    }

    static async Task<SagaStorageFile> OpenWithRetryOnConcurrency(string filePath, FileMode fileAccess, JsonSerializerOptions options, CancellationToken cancellationToken)
    {
        var numRetries = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return new SagaStorageFile(new FileStream(filePath, fileAccess, FileAccess.ReadWrite, FileShare.None, DefaultBufferSize, FileOptions.Asynchronous), options);
            }
            catch (IOException)
            {
                numRetries++;

                if (numRetries > 4) // Given the 100ms delay below, we wait roughly 500 ms for the file to become unlocked
                {
                    throw;
                }

                // Give the other task some time to complete the saga to avoid retrying too much
                await Task.Delay(100, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    public async Task Write(IContainSagaData sagaData, IReadOnlyList<StoredSagaTimeout> timeouts, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(fileStream is null, this);

        fileStream.Position = 0;

        var sagaDataType = sagaData.GetType();
        var typeInfo = options.ResolveTypeInfo(sagaDataType);

        // Sagas without timeouts keep the plain saga data format
        if (timeouts.Count == 0)
        {
            if (typeInfo is not null)
            {
                await JsonSerializer.SerializeAsync(fileStream, sagaData, typeInfo, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await SerializeWithReflectionAsync(fileStream, sagaData, sagaDataType, options, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        else
        {
            using var writer = new Utf8JsonWriter(fileStream);
            writer.WriteStartObject();
            writer.WritePropertyName(TimeoutsPropertyName);
            JsonSerializer.Serialize(writer, [.. timeouts], SagaTimeoutJsonContext.Default.ListStoredSagaTimeout);
            writer.WritePropertyName(SagaDataPropertyName);
            if (typeInfo is not null)
            {
                JsonSerializer.Serialize(writer, sagaData, typeInfo);
            }
            else
            {
                SerializeWithReflection(writer, sagaData, sagaDataType, options);
            }
            writer.WriteEndObject();
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        // Because the file is opened in ReadWrite mode, leftover content from last write
        // could be left behind if the new content is shorter.
        fileStream.SetLength(fileStream.Position);
    }

    public void MarkAsCompleted() => isCompleted = true;

    public async ValueTask<(TSagaData? SagaData, List<StoredSagaTimeout> Timeouts)> Read<TSagaData>(CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData
    {
        ObjectDisposedException.ThrowIf(fileStream is null, this);

        using var document = await JsonDocument.ParseAsync(fileStream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var sagaDataElement = document.RootElement;
        List<StoredSagaTimeout> timeouts = [];

        if (sagaDataElement.ValueKind == JsonValueKind.Object && sagaDataElement.TryGetProperty(SagaDataPropertyName, out var envelopedSagaData))
        {
            if (sagaDataElement.TryGetProperty(TimeoutsPropertyName, out var timeoutsElement))
            {
                timeouts = timeoutsElement.Deserialize(SagaTimeoutJsonContext.Default.ListStoredSagaTimeout) ?? [];
            }

            sagaDataElement = envelopedSagaData;
        }

        var typeInfo = options.ResolveTypeInfo(typeof(TSagaData));
        var sagaData = typeInfo is not null
            ? (TSagaData?)sagaDataElement.Deserialize(typeInfo)
            : (TSagaData?)DeserializeWithReflection(sagaDataElement, typeof(TSagaData), options);

        return (sagaData, timeouts);
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Only called when System.Text.Json reflection serialization is enabled.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification = "Only called when System.Text.Json reflection serialization is enabled.")]
    static Task SerializeWithReflectionAsync(Stream stream, object value, Type inputType, JsonSerializerOptions options, CancellationToken cancellationToken)
        => JsonSerializer.SerializeAsync(stream, value, inputType, options, cancellationToken);

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Only called when System.Text.Json reflection serialization is enabled.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification = "Only called when System.Text.Json reflection serialization is enabled.")]
    static void SerializeWithReflection(Utf8JsonWriter writer, object value, Type inputType, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, inputType, options);

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Only called when System.Text.Json reflection serialization is enabled.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification = "Only called when System.Text.Json reflection serialization is enabled.")]
    static object? DeserializeWithReflection(JsonElement element, Type returnType, JsonSerializerOptions options)
        => element.Deserialize(returnType, options);

    FileStream? fileStream;
    readonly JsonSerializerOptions options;
    bool isCompleted;

    const int DefaultBufferSize = 4096;
    const string SagaDataPropertyName = "$sagaData";
    const string TimeoutsPropertyName = "$sagaTimeouts";
    static readonly Task<SagaStorageFile?> noSagaFoundResult = Task.FromResult<SagaStorageFile?>(null);
}