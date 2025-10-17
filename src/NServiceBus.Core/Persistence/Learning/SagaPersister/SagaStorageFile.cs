#nullable enable

namespace NServiceBus;

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

class SagaStorageFile : IDisposable, IAsyncDisposable
{
    SagaStorageFile(FileStream fileStream) => this.fileStream = fileStream;

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

        return OpenWithRetryOnConcurrency(filePath, FileMode.Open, cancellationToken)!;
    }

    public static Task<SagaStorageFile> Create(Guid sagaId, SagaManifest manifest, CancellationToken cancellationToken = default)
    {
        var filePath = manifest.GetFilePath(sagaId);

        return OpenWithRetryOnConcurrency(filePath, FileMode.CreateNew, cancellationToken);
    }

    static async Task<SagaStorageFile> OpenWithRetryOnConcurrency(string filePath, FileMode fileAccess, CancellationToken cancellationToken)
    {
        var numRetries = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return new SagaStorageFile(new FileStream(filePath, fileAccess, FileAccess.ReadWrite, FileShare.None, DefaultBufferSize, FileOptions.Asynchronous));
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
        await JsonSerializer.SerializeAsync(fileStream, sagaData, sagaData.GetType(), Options, cancellationToken)
            .ConfigureAwait(false);

        // Because the file is opened in ReadWrite mode, leftover content from last write
        // could be left behind if the new content is shorter.
        fileStream.SetLength(fileStream.Position);
    }

    public void MarkAsCompleted() => isCompleted = true;

    public ValueTask<TSagaData?> Read<TSagaData>(CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData
    {
        ObjectDisposedException.ThrowIf(fileStream is null, this);

        return JsonSerializer.DeserializeAsync<TSagaData>(fileStream, Options, cancellationToken);
    }

    FileStream? fileStream;
    bool isCompleted;

    const int DefaultBufferSize = 4096;
    static readonly Task<SagaStorageFile?> noSagaFoundResult = Task.FromResult<SagaStorageFile?>(null);

    static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };
}