namespace NServiceBus.Persistence.InMemory;

using System.Text.Json;

/// <summary>
/// Holds settings for the InMemorySagaPersister to support constructor injection.
/// </summary>
public class InMemorySagaPersisterSettings(JsonSerializerOptions serializerOptions)
{
    public JsonSerializerOptions SerializerOptions { get; } = serializerOptions;
}