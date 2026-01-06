#nullable enable

namespace NServiceBus;

using System;

/// <summary>
/// Contains configuration extensions for OpenTelemetry support.
/// </summary>
public static partial class OpenTelemetryConfigurationExtensions
{
    /// <summary>
    /// Disables the OpenTelemetry instrumentation in NServiceBus.
    /// </summary>
    public static void DisableOpenTelemetry(this EndpointConfiguration endpointConfiguration)
    {
        ArgumentNullException.ThrowIfNull(endpointConfiguration);

        endpointConfiguration.Settings.Get<HostingComponent.Settings>().EnableOpenTelemetry = false;
        endpointConfiguration.DisableFeature<OpenTelemetryFeature>();
    }
}