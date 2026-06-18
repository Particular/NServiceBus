#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

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

    public async Task Write(IContainSagaData sagaData, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(fileStream is null, this);

        fileStream.Position = 0;

        var sagaDataType = sagaData.GetType();
        var typeInfo = ResolveTypeInfo(sagaDataType, options);
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

        // Because the file is opened in ReadWrite mode, leftover content from last write
        // could be left behind if the new content is shorter.
        fileStream.SetLength(fileStream.Position);
    }

    public void MarkAsCompleted() => isCompleted = true;

    public ValueTask<TSagaData?> Read<TSagaData>(CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData
    {
        ObjectDisposedException.ThrowIf(fileStream is null, this);

        var typeInfo = ResolveTypeInfo(typeof(TSagaData), options);
        if (typeInfo is not null)
        {
            return ReadWithTypeInfoAsync<TSagaData>(fileStream, typeInfo, cancellationToken);
        }
        else
        {
            return DeserializeWithReflectionAsync<TSagaData>(fileStream, options, cancellationToken);
        }
    }

    static async ValueTask<TSagaData?> ReadWithTypeInfoAsync<TSagaData>(Stream stream, JsonTypeInfo typeInfo, CancellationToken cancellationToken) where TSagaData : class
        => (TSagaData?)await JsonSerializer.DeserializeAsync(stream, typeInfo, cancellationToken).ConfigureAwait(false);

    static JsonTypeInfo? ResolveTypeInfo(Type runtimeType, JsonSerializerOptions? options)
    {
        if (options is null)
        {
            return null;
        }

        var typeInfo = options.TypeInfoResolver?.GetTypeInfo(runtimeType, options);
        if (typeInfo is not null)
        {
            return typeInfo;
        }

        return JsonSerializer.IsReflectionEnabledByDefault ? null : throw new InvalidOperationException($"No JSON metadata was found for '{runtimeType.FullName}'.");
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
    static async ValueTask<TSagaData?> DeserializeWithReflectionAsync<TSagaData>(Stream stream, JsonSerializerOptions options, CancellationToken cancellationToken) where TSagaData : class
        => (TSagaData?)await JsonSerializer.DeserializeAsync(stream, typeof(TSagaData), options, cancellationToken).ConfigureAwait(false);

    FileStream? fileStream;
    readonly JsonSerializerOptions options;
    bool isCompleted;

    const int DefaultBufferSize = 4096;
    static readonly Task<SagaStorageFile?> noSagaFoundResult = Task.FromResult<SagaStorageFile?>(null);
}