namespace NServiceBus;

/// <summary>
/// Contains configuration extensions for OpenTelemetry support.
/// </summary>
public static class OpenTelemetryConfigurationExtensions
{
    /// <summary>
    /// Enables the OpenTelemetry instrumentation in NServiceBus.
    /// </summary>
    public static void EnableOpenTelemetry(this EndpointConfiguration endpointConfiguration)
    {
        Guard.ThrowIfNull(endpointConfiguration);

        endpointConfiguration.Settings.Get<HostingComponent.Settings>().EnableOpenTelemetry = true;
        endpointConfiguration.EnableFeature<MessagingMetricsFeature>();
    }
}