#nullable enable

namespace NServiceBus;

using System.Collections.Generic;

sealed class SerializationDiagnostics
{
    public required MainSerializerDiagnostics MainSerializer { get; init; }
    public required List<AdditionalDeserializerDiagnostics> AdditionalDeserializers { get; init; }
    public required bool AllowMessageTypeInference { get; init; }
}

sealed class MainSerializerDiagnostics
{
    public required string Type { get; init; }
    public required string Version { get; init; }
    public required string ContentType { get; init; }
}

sealed class AdditionalDeserializerDiagnostics
{
    public required string Type { get; init; }
    public required string Version { get; init; }
    public required string ContentType { get; init; }
}
