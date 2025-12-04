#nullable enable

namespace NServiceBus;

using System;
using System.IO;

class SagaManifest
{
    public required string StorageDirectory { get; init; }
    public required Type SagaEntityType { get; init; }

    public string GetFilePath(Guid sagaId) => Path.Combine(StorageDirectory, sagaId + ".json");
}