#nullable enable

namespace NServiceBus;

using System;
using System.IO;
using System.Text.Json;

class SagaManifest
{
    public required string StorageDirectory { get; init; }
    public required Type SagaEntityType { get; init; }
    public required JsonSerializerOptions SerializerOptions { get; init; }

    public string GetFilePath(Guid sagaId) => Path.Combine(StorageDirectory, sagaId + ".json");
}