#nullable enable

namespace NServiceBus;

using System;

/// <summary>
/// Contains configuration extensions for OpenTelemetry support.
/// </summary>
public static class OpenTelemetryConfigurationExtensions
{
    /// <summary>
    /// Enables the OpenTelemetry instrumentation in NServiceBus.
    /// </summary>
    [Obsolete("OpenTelemetry is now enabled by default. This method is no longer required and will be removed in a future version.")]
    public static void EnableOpenTelemetry(this EndpointConfiguration endpointConfiguration)
    {
        ArgumentNullException.ThrowIfNull(endpointConfiguration);

        endpointConfiguration.Settings.Get<HostingComponent.Settings>().EnableOpenTelemetry = true;
        endpointConfiguration.EnableFeature<OpenTelemetryFeature>();
    }

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