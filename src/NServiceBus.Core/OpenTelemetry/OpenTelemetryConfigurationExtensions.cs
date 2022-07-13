namespace NServiceBus.OpenTelemetry;

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
        Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);

        endpointConfiguration.Settings.Get<HostingComponent.Settings>().EnableOpenTelemetry = true;
        endpointConfiguration.EnableFeature<MessagingMetricsFeature>();
    }
}