#nullable enable

namespace NServiceBus;

using System;

/// <summary>
/// Contains configuration extensions for OpenTelemetry support.
/// </summary>
public static partial class OpenTelemetryConfigurationExtensions
{
    /// <summary>
    /// Prevents NServiceBus from creating OpenTelemetry traces for messages.
    /// This opts out of activity creation; it does not change whether an `ActivitySource` has listeners.
    /// </summary>
    /// <remarks>
    /// By default, NServiceBus endpoints generate OpenTelemetry traces for incoming and outgoing messages when the `NServiceBus.Core`
    /// activity source is configured. Disabling here stops creating Activity instances regardless of whether the `NServiceBus.Core` activity source
    /// is configured in the OpenTelemetry tracer. To capture traces, add the `NServiceBus.Core` activity source:
    /// <code>
    /// var tracingProviderBuilder = Sdk.CreateTracerProviderBuilder()
    ///     .AddSource("NServiceBus.Core")
    ///     // ... Add other trace sources and exporters
    ///     .Build();
    /// </code>
    /// </remarks>
    public static void DisableOpenTelemetry(this EndpointConfiguration endpointConfiguration)
    {
        ArgumentNullException.ThrowIfNull(endpointConfiguration);

        endpointConfiguration.Settings.Get<HostingComponent.Settings>().EnableOpenTelemetry = false;
        endpointConfiguration.DisableFeature<OpenTelemetryFeature>();
    }
}