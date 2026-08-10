#nullable enable

namespace NServiceBus;

/// <summary>
/// Controls opt-in meter instruments behaviors.
/// Accessed via <c>endpointConfiguration.Tracing().Meters</c>.
/// </summary>
public class MetersOptions
{
    /// <summary>
    /// Emits the legacy <c>execution.result</c> tag with values <c>"success"</c> or <c>"failure"</c>
    /// on handler time, processing time, saga fetch time, deserialize time, and serialize time metrics.
    /// Enabled by default for backwards compatibility. Disable to reduce tag cardinality.
    /// </summary>
    public bool EmitExecutionResultTags { get; set; } = true;
}
