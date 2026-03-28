namespace NServiceBus.Persistence.InMemory;

using System.Text.Json;

/// <summary>
/// Represents a stored saga entry with versioning for optimistic concurrency control.
/// Uses System.Text.Json for deep copying to ensure AOT/trimming compatibility.
/// </summary>
class SagaEntry(IContainSagaData sagaData, CorrelationId correlationId, int version, JsonSerializerOptions serializerOptions)
{
    public CorrelationId CorrelationId { get; } = correlationId;

    public int Version { get; } = version;

    /// <summary>
    /// Creates a deep copy of the saga data using System.Text.Json serialization.
    /// This approach is AOT and trimming compatible.
    /// </summary>
    public IContainSagaData GetSagaCopy()
    {
        var type = sagaData.GetType();
        var json = JsonSerializer.Serialize(sagaData, type, serializerOptions);
        return (IContainSagaData)JsonSerializer.Deserialize(json, type, serializerOptions)!;
    }

    public SagaEntry UpdateTo(IContainSagaData newSagaData, JsonSerializerOptions newSerializerOptions)
        => new(newSagaData, CorrelationId, Version + 1, newSerializerOptions);
}
