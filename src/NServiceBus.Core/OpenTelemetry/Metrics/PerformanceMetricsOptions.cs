#nullable enable

namespace NServiceBus;

/// <summary>
/// Controls opt-in performance metrics instrumentation.
/// Accessed via <c>endpointConfiguration.PerformanceMetrics()</c>.
/// </summary>
public class PerformanceMetricsOptions
{
    /// <summary>
    /// Enables the <c>nservicebus.sagas.fetch_time</c> histogram.
    /// Records the time taken to load saga data from the persister.
    /// </summary>
    public bool EnableSagaFetchTime { get; set; }

    /// <summary>
    /// Enables the <c>nservicebus.messaging.deserialize_time</c> histogram.
    /// Records the time taken to deserialize an incoming message body.
    /// </summary>
    public bool EnableDeserializeTime { get; set; }

    /// <summary>
    /// Enables the <c>nservicebus.messaging.serialize_time</c> histogram.
    /// Records the time taken to serialize an outgoing message body.
    /// </summary>
    public bool EnableSerializeTime { get; set; }

    /// <summary>
    /// Enables the <c>nservicebus.outbox.fetch_time</c> histogram.
    /// Records the time taken to query the outbox storage for deduplication.
    /// </summary>
    public bool EnableOutboxFetchTime { get; set; }

    /// <summary>
    /// Enables the <c>nservicebus.outbox.store_time</c> histogram.
    /// Records the time taken to store a message in the outbox storage.
    /// </summary>
    public bool EnableOutboxStoreTime { get; set; }

    /// <summary>
    /// Enables the <c>nservicebus.persistence.time</c> histogram.
    /// Records the time taken to complete the synchronized storage session.
    /// </summary>
    public bool EnablePersistenceTime { get; set; }
}
